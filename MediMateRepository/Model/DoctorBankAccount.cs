using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediMateRepository.Model
{
    public class DoctorBankAccount
    {
        public Guid BankAccountId { get; set; }
        public Guid DoctorId { get; set; }
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string AccountHolder { get; set; }
        public DateTime CreatedAt { get; set; }
        public virtual Doctors Doctor { get; set; }
    }
}
