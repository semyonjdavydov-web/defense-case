using System.ComponentModel.DataAnnotations;

namespace DefenseCase.Web.Models;

// The original five console fields are retained, with the needed court details.
public class CaseInformation
{
    [Required, StringLength(150), Display(Name = "Defendant name")]
    public string DefendantName { get; set; } = "";
    [Required, StringLength(100), Display(Name = "Case number")]
    public string CaseNumber { get; set; } = "";
    [Required, StringLength(200)]
    public string Court { get; set; } = "";
    [Required, StringLength(80)]
    public string County { get; set; } = "";
    [Required, StringLength(150), Display(Name = "Attorney name")]
    public string AttorneyName { get; set; } = "";
    [Required, StringLength(1500), Display(Name = "Charges")]
    public string Charge { get; set; } = "";
    [Required, RegularExpression("Felony|Misdemeanor90|Misdemeanor60|Violation"), Display(Name = "Highest charge classification")]
    public string HighestChargeClassification { get; set; } = "";
    [Required, RegularExpression("LocalCriminalCourt|Indictment"), Display(Name = "Accusatory instrument")]
    public string InstrumentType { get; set; } = "";
    [Required, DataType(DataType.Date), Display(Name = "Motion date")]
    public DateTime? MotionDate { get; set; }
    public SpeedyTrialInformation SpeedyTrial { get; set; } = new();

    public MotionGenerator.Case ToConsoleCase() => new()
    {
        DefendantName = DefendantName, CaseNumber = CaseNumber, Court = Court,
        AttorneyName = AttorneyName, Charge = Charge
    };
}

// These are counsel's inputs. The program never calculates chargeable time.
public class SpeedyTrialInformation
{
    [Required, DataType(DataType.Date), Display(Name = "When was the defendant arraigned?")]
    public DateTime? ArraignmentDate { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "Date prosecution filed Certificate of Compliance")]
    public DateTime? CertificateOfComplianceDate { get; set; }
    [Required, Range(0, int.MaxValue), Display(Name = "How many days are chargeable to the prosecution?")]
    public int? ProsecutionChargeableDays { get; set; }
}
