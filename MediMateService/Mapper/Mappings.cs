using AutoMapper;
using MediMateRepository.Model;
using MediMateService.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateService.Mapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // --- 1. USER & MEMBER ---

            // Map Entity -> Response
            CreateMap<User, UserProfileResponse>();

            CreateMap<Members, MemberResponse>();
                //.ForMember(dest => dest.HasHealthProfile, opt => opt.MapFrom(src => src.HealthProfile != null)); // VD: Map thêm field tính toán

            // Map Request -> Entity (Update)
            // Sử dụng Condition để chỉ map khi giá trị không null (cho tính năng Partial Update)
            CreateMap<UpdateProfileRequest, User>()
                .ForMember(dest => dest.AvatarUrl, opt => opt.Ignore()) // Avatar xử lý riêng (upload file)
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<UpdateMemberRequest, Members>()
                .ForMember(dest => dest.AvatarUrl, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // --- 2. FAMILY ---
            CreateMap<Families, FamilyResponse>();
            CreateMap<UpdateFamilyRequest, Families>();

            // --- 3. HEALTH PROFILE ---

            // Entity -> Response
            CreateMap<HealthProfiles, HealthProfileResponse>()
                .ForMember(dest => dest.BMI, opt => opt.MapFrom(src => CalculateBmi(src.Height, src.Weight)));

            CreateMap<HealthConditions, HealthConditionDto>().ReverseMap(); // Map 2 chiều

            // Request -> Entity
            CreateMap<CreateHealthProfileRequest, HealthProfiles>();
            CreateMap<UpdateHealthProfileRequest, HealthProfiles>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) =>
                {
                    // Logic: Chỉ map nếu không null. Với kiểu số (double), check > 0
                    if (srcMember is double d) return d > 0;
                    return srcMember != null;
                }));

            CreateMap<AddConditionRequest, HealthConditions>();
            CreateMap<UpdateConditionRequest, HealthConditions>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));


            // --- 4. PRESCRIPTIONS (Phức tạp nhất) ---

            // Map con: Image & Medicine
            CreateMap<PrescriptionImages, PrescriptionImageDto>()
                .ReverseMap(); // Dùng ReverseMap để map ngược từ DTO -> Entity

            CreateMap<PrescriptionMedicines, PrescriptionMedicineResponse>()
                .ReverseMap();

            // Map cha: Prescription
            CreateMap<Prescriptions, PrescriptionResponse>()
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.PrescriptionImages))
                .ForMember(dest => dest.Medicines, opt => opt.MapFrom(src => src.PrescriptionMedicines)); // Lưu ý tên property trong Entity là PrescriptionMedicines

            // Map Request -> Entity (Create)
            CreateMap<CreatePrescriptionRequest, Prescriptions>()
                .ForMember(dest => dest.PrescriptionImages, opt => opt.MapFrom(src => src.Images))
                .ForMember(dest => dest.PrescriptionMedicines, opt => opt.MapFrom(src => src.Medicines))
                .ForMember(dest => dest.PrescriptionId, opt => opt.Ignore()) // ID tự sinh
                .ForMember(dest => dest.CreateAt, opt => opt.Ignore());

            // Map Request -> Entity (Update)
            CreateMap<UpdatePrescriptionRequest, Prescriptions>()
                .ForMember(dest => dest.PrescriptionMedicines, opt => opt.Ignore()) // List thuốc xử lý tay (xóa cũ thêm mới)
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }

        // Helper tính BMI
        private double CalculateBmi(double height, double weight)
        {
            if (height <= 0) return 0;
            // Giả sử Height lưu cm, Weight lưu kg
            double h = height / 100.0;
            return Math.Round(weight / (h * h), 2);
        }
    }
}
