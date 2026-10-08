using DefenseCase.Web.Models;
using MotionGenerator;

namespace DefenseCase.Web.Services;

public class DocumentGenerationService
{
    public byte[] Generate(CaseInformation caseInfo, DiscoveryAnalysisResult discovery, LegalArgumentResult argument)
    {
        var content = new MotionContent
        {
            MotionTitle = argument.SupportsDismissal ? "NOTICE OF MOTION TO DISMISS" : "NOTICE OF MOTION FOR DISCOVERY RELIEF",
            OpeningRequest = "PLEASE TAKE NOTICE that the defendant, " + caseInfo.DefendantName + ", by attorney " + caseInfo.AttorneyName +
                ", respectfully moves this Court for the relief set forth below pursuant to CPL Article 245" +
                (argument.SupportsDismissal ? " and CPL 30.30." : "."),
            MissingDiscoveryIntroduction = discovery.MissingItems.Count > 0
                ? "The following expected discovery was not located in the discovery packet supplied for analysis:"
                : "The submitted packet does not establish a missing applicable discovery item.",
            BackgroundParagraphs = ["Defendant was arraigned on " + caseInfo.SpeedyTrial.ArraignmentDate!.Value.ToString("MMMM d, yyyy") +
                ". The People filed their Certificate of Compliance on " + caseInfo.SpeedyTrial.CertificateOfComplianceDate!.Value.ToString("MMMM d, yyyy") +
                ". The People are chargeable with " + caseInfo.SpeedyTrial.ProsecutionChargeableDays + " days pursuant to CPL 30.30."],
            PreliminaryArgument = argument.PreliminaryTimelinessArgument,
            PointOneHeading = "POINT I — " + argument.PointOneHeading, PointOneParagraphs = argument.PointOneParagraphs,
            PointTwoHeading = argument.SupportsDismissal ? "POINT II — " + argument.PointTwoHeading : "", PointTwoParagraphs = argument.PointTwoParagraphs,
            RequestedRelief = argument.RequestedRelief, MotionDate = caseInfo.MotionDate!.Value
        };
        var missing = discovery.MissingItems.Select(item => new DiscoveryItem
        {
            IsMissing = true, Name = item.ItemName + " (" + item.StatutoryBasis + "). " + item.ReasonExpected + " " + item.LegalSignificance
        }).ToList();
        return new TemplateGenerator().CreateMotion(caseInfo.ToConsoleCase(), missing, content);
    }
}
