using System;
using System.Collections.Generic;

namespace Fluxion_Lab.Models.Masters.DoctorMaster
{
    public class DoctorWeeklySchedule
    {
        public byte DayOfWeek { get; set; }
        public string DayOfWeekNName { get; set; }
        public bool IsAvailable { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }

    public class DoctorMasterWithSchedule
    {
        public int? ID { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public string MobileNo { get; set; }
        public string Address { get; set; }
        public string PersonalDetails { get; set; }
        public string ImageUrl { get; set; }
        public int? SlotDuration { get; set; }
        public decimal? NewRegistrationFee { get; set; }
        public decimal? RenewRegistrationFee { get; set; } 
        public decimal? NewConsultFee { get; set; }
        public decimal? RenewConsultFee { get; set; }
        public int? ValidityDays { get; set; }
        public decimal? CutsAmount { get; set; }
        public decimal? Commission { get; set; }
        public decimal? Discount { get; set; } 
        public List<DoctorWeeklySchedule> WeeklySchedule { get; set; }
    }

    public class DoctorMasterGetDto
    {
        public int? DoctorID { get; set; }
        public string DoctorName { get; set; }
        public string Designation { get; set; }
        public string Department { get; set; }
        public string MobileNo { get; set; }
        public string Address { get; set; }
        public string PersonalDetails { get; set; }
        public string ImageUrl { get; set; }
        public int? SlotDuration { get; set; }
        public decimal? NewRegistrationFee { get; set; }
        public decimal? RenewRegistrationFee { get; set; }
        public decimal? NewConsultFee { get; set; }
        public decimal? RenewConsultFee { get; set; }
        public int? ValidityDays { get; set; }
        public decimal? CutsAmount { get; set; }
        public decimal? Commission { get; set; }
        public decimal? Discount { get; set; }

        public List<DoctorWeeklySchedule> WeeklySchedule { get; set; }
        public List<AppointmentRescheduleHistoryDto> RescheduleHistory { get; set; } = new List<AppointmentRescheduleHistoryDto>();
    }

    public class AppointmentRescheduleHistoryDto
    {
        public long RescheduleID { get; set; }
        public int DoctorID { get; set; }
        public string Department { get; set; }
        public string ResheduledFrom { get; set; }
        public string ResheduledTo { get; set; } 
        public string ResheduledFromTime { get; set; }
        public string ResheduleToTime { get; set; } 
        public string CreatedAt { get; set; }
    }
}
