namespace Fluxion_Lab.Models.EventManagment
{
    public class Registration
    {
        public Participant Participant { get; set; }
    }

    public class Participant
    {
        public string ParticipantName { get; set; }
        public string ContactNo { get; set; }
        public string EmailID { get; set; }
        public string Designation { get; set; }
        public int CollegeID { get; set; }
        public string FoodType { get; set; }
        public string? OtherName { get; set; }
    } 
    
}
