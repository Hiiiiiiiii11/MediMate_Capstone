using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using MediMateRepository.Model;
using MediMateRepository.Repositories;
using MediMateService.DTOs;
using Microsoft.EntityFrameworkCore;
using Share.Common;

namespace MediMateService.Services.Implementations
{
    public class DrugDataService : IDrugDataService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DrugDataService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<object>> ImportDrugsFromXmlAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return ApiResponse<object>.Fail("File không tồn tại trên hệ thống.", 400);

            int batchSize = 100;
            int totalDrugsInserted = 0;
            int currentBatchCount = 0;

            try
            {
                using (XmlReader reader = XmlReader.Create(filePath))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "drug")
                        {
                            var typeAttribute = reader.GetAttribute("type");
                            if (typeAttribute == "biotech" || typeAttribute == "small molecule")
                            {
                                var drug = ParseDrug(reader);
                                if (drug != null && !string.IsNullOrEmpty(drug.Name))
                                {
                                    await _unitOfWork.Repository<Drug>().AddAsync(drug);
                                    currentBatchCount++;
                                    totalDrugsInserted++;

                                    if (currentBatchCount >= batchSize)
                                    {
                                        await _unitOfWork.CompleteAsync();
                                        currentBatchCount = 0;
                                    }
                                }
                            }
                        }
                    }
                }
                
                if (currentBatchCount > 0)
                {
                    await _unitOfWork.CompleteAsync();
                }

                return ApiResponse<object>.Ok(new { TotalImported = totalDrugsInserted }, $"Đã cào dữ liệu và import thành công {totalDrugsInserted} loại thuốc vào hệ thống.");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"Lỗi trong lúc import data: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<List<DrugDto>>> SearchDrugsAsync(string searchTerm, int limit = 10)
        {
            try
            {
                var query = _unitOfWork.Repository<Drug>().GetQueryable().AsNoTracking();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(d => d.Name.ToLower().Contains(searchTerm) || d.Synonyms.ToLower().Contains(searchTerm));
                }

                var drugs = await query
                    .OrderBy(d => d.Name)
                    .Take(limit)
                    .Select(d => new DrugDto
                    {
                        DrugId = d.DrugId,
                        DrugBankId = d.DrugBankId,
                        Name = d.Name,
                        Synonyms = d.Synonyms,
                        Description = d.Description
                    })
                    .ToListAsync();

                return ApiResponse<List<DrugDto>>.Ok(drugs, "Tìm kiếm thuốc thành công.");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<DrugDto>>.Fail($"Lỗi hệ thống: {ex.Message}", 500);
            }
        }

        private Drug ParseDrug(XmlReader reader)
        {
            Drug drug = new Drug { DrugId = Guid.NewGuid() };
            
            using (XmlReader inner = reader.ReadSubtree())
            {
                while (inner.Read())
                {
                    if (inner.NodeType == XmlNodeType.Element)
                    {
                        if (inner.Name == "drugbank-id" && string.IsNullOrEmpty(drug.DrugBankId)) 
                        {
                            string isPrimary = inner.GetAttribute("primary");
                            if (isPrimary == "true")
                            {
                                drug.DrugBankId = inner.ReadElementContentAsString();
                            }
                        }
                        else if (inner.Name == "name" && string.IsNullOrEmpty(drug.Name))
                        {
                            drug.Name = inner.ReadElementContentAsString();
                        }
                        else if (inner.Name == "description" && string.IsNullOrEmpty(drug.Description))
                        {
                            var desc = inner.ReadElementContentAsString();
                            drug.Description = desc.Length > 2000 ? desc.Substring(0, 1997) + "..." : desc;
                        }
                        else if (inner.Name == "synonyms" && string.IsNullOrEmpty(drug.Synonyms))
                        {
                            var synonymsList = new List<string>();
                            using (XmlReader synReader = inner.ReadSubtree())
                            {
                                while (synReader.Read())
                                {
                                    if (synReader.NodeType == XmlNodeType.Element && synReader.Name == "synonym")
                                    {
                                        var syn = synReader.ReadElementContentAsString();
                                        if (!string.IsNullOrWhiteSpace(syn))
                                        {
                                            synonymsList.Add(syn.Trim());
                                        }
                                    }
                                }
                            }
                            drug.Synonyms = string.Join(", ", synonymsList);
                        }
                        else if (inner.Name == "drug-interactions")
                        {
                            drug.DrugInteractions = ParseInteractions(inner, drug.DrugId);
                        }
                    }
                }
            }
            
            return drug;
        }

        private List<DrugInteraction> ParseInteractions(XmlReader reader, Guid drugId)
        {
            List<DrugInteraction> interactions = new List<DrugInteraction>();
            using (XmlReader inner = reader.ReadSubtree())
            {
                while (inner.Read())
                {
                    if (inner.NodeType == XmlNodeType.Element && inner.Name == "drug-interaction")
                    {
                        var interaction = new DrugInteraction
                        {
                            InteractionId = Guid.NewGuid(),
                            DrugId = drugId
                        };

                        using (XmlReader intReader = inner.ReadSubtree())
                        {
                            while (intReader.Read())
                            {
                                if (intReader.NodeType == XmlNodeType.Element)
                                {
                                    if (intReader.Name == "drugbank-id")
                                    {
                                        interaction.InteractingDrugBankId = intReader.ReadElementContentAsString();
                                    }
                                    else if (intReader.Name == "name")
                                    {
                                        interaction.InteractingDrugName = intReader.ReadElementContentAsString();
                                    }
                                    else if (intReader.Name == "description")
                                    {
                                        var desc = intReader.ReadElementContentAsString();
                                        interaction.Description = desc.Length > 1000 ? desc.Substring(0, 997) + "..." : desc;
                                    }
                                }
                            }
                        }
                        interactions.Add(interaction);
                    }
                }
            }
            return interactions;
        }
    }
}
