using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MotionGenerator;
using System.Xml;

public class TemplateGenerator
{
    // Create the Word motion from the case details and missing discovery.
    public void CreateMotion(Case caseInfo, List<DiscoveryItem> missingItems, string filePath)
    {
        // Keep the original console motion wording and save location.
        MotionContent content = new MotionContent();
        content.MotionTitle = "NOTICE OF MOTION TO DISMISS\nFOR FAILURE TO PROVIDE DISCOVERY";
        content.OpeningRequest = "PLEASE TAKE NOTICE that the defendant, " + caseInfo.DefendantName;
        content.OpeningRequest += ", by attorney " + caseInfo.AttorneyName;
        content.OpeningRequest += ", respectfully moves this Court for dismissal based on the prosecution's failure ";
        content.OpeningRequest += "to provide required discovery within the time allowed by CPL Article 245.";
        content.MissingDiscoveryIntroduction = "The following discovery remains outstanding:";
        content.RequestedRelief = "Because these materials have not been provided, the defense respectfully requests ";
        content.RequestedRelief += "that the Court dismiss the case, or grant any other relief the Court finds appropriate.";
        content.MotionDate = DateTime.Today;

        // Create a new Word document.
        using (WordprocessingDocument document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
        {
            WriteMotion(document, caseInfo, missingItems, content);
        }
    }

    // Return the web motion in memory so a browser can download it without saving case files.
    public byte[] CreateMotion(Case caseInfo, List<DiscoveryItem> missingItems, MotionContent content)
    {
        using MemoryStream stream = new MemoryStream();
        using (WordprocessingDocument document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            WriteMotion(document, caseInfo, missingItems, content);
        }

        return stream.ToArray();
    }

    // Both versions use the same caption, paragraph formatting, and signature.
    static void WriteMotion(WordprocessingDocument document, Case caseInfo, List<DiscoveryItem> missingItems, MotionContent content)
    {
        MainDocumentPart mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document();

        Body body = new Body();
        mainPart.Document.Append(body);

        // Add the court caption to the document
        AddCenteredParagraph(body, (caseInfo.Court ?? string.Empty).ToUpper(), true, false);
        AddCenteredParagraph(body, "STATE OF NEW YORK", false, false);
        AddCenteredParagraph(body, "------------------------------------------------------------X", false, false);
        AddParagraph(body, "THE PEOPLE OF THE STATE OF NEW YORK,", false, false, false);
        AddCenteredParagraph(body, "-against-", false, false);
        AddParagraph(body, (caseInfo.DefendantName ?? string.Empty).ToUpper() + ",", false, false, false);
        AddParagraph(body, "Defendant.", false, false, true);
        AddCenteredParagraph(body, "------------------------------------------------------------X", false, false);

        // Add the name of the motion
        foreach (string titleLine in content.MotionTitle.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            AddCenteredParagraph(body, titleLine.TrimEnd('\r'), true, true);
        }

        // Add the case information
        AddParagraph(body, "Case Number: " + caseInfo.CaseNumber, true, false, false);
        AddParagraph(body, "Charge: " + caseInfo.Charge, true, false, false);

        // Add the opening paragraph and the list of missing discovery items to the document
        AddParagraph(body, content.OpeningRequest, false, false, false);

        // Place the supplied case facts before the missing discovery list.
        foreach (string background in content.BackgroundParagraphs)
        {
            AddParagraph(body, background, false, false, false);
        }
        if (!string.IsNullOrWhiteSpace(content.MissingDiscoveryIntroduction))
        {
            AddParagraph(body, content.MissingDiscoveryIntroduction, true, false, false);
        }

        // Add each missing discovery item to the document
        for (int i = 0; i < missingItems.Count; i++)
        {
            AddMissingItem(body, i + 1, missingItems[i].Name);
        }

        // Place the legal argument immediately after the facts and missing discovery.
        if (!string.IsNullOrWhiteSpace(content.PreliminaryArgument))
        {
            AddParagraph(body, content.PreliminaryArgument, false, false, false);
        }
        if (!string.IsNullOrWhiteSpace(content.PointOneHeading))
        {
            AddCenteredParagraph(body, content.PointOneHeading, true, true, true);
        }
        foreach (string paragraph in content.PointOneParagraphs)
        {
            AddParagraph(body, paragraph, false, false, false);
        }
        if (!string.IsNullOrWhiteSpace(content.PointTwoHeading))
        {
            AddCenteredParagraph(body, content.PointTwoHeading, true, true, true);
        }
        foreach (string paragraph in content.PointTwoParagraphs)
        {
            AddParagraph(body, paragraph, false, false, false);
        }

        AddParagraph(body, content.RequestedRelief, false, false, false);
        AddParagraph(body, "Dated: " + content.MotionDate.ToString("MMMM d, yyyy"), false, false, false);
        AddParagraph(body, "Respectfully submitted,", false, false, false);
        AddParagraph(body, caseInfo.AttorneyName, true, false, false);

        // Add the page size and margins to the end of the document
        AddPageSettings(body);

        // Save the finished Word document
        mainPart.Document.Save();
    }

    // Add a regular paragraph to the document

    static void AddParagraph(Body body, string? text, bool bold, bool underline, bool rightAligned)
    {
        Paragraph paragraph = new Paragraph();
        ParagraphProperties paragraphProperties = new ParagraphProperties();

        SpacingBetweenLines spacing = new SpacingBetweenLines();
        spacing.After = "120";
        spacing.Line = "264";
        spacing.LineRule = LineSpacingRuleValues.Auto;

        paragraphProperties.Append(spacing);

        if (rightAligned == true)
        {
            Justification justification = new Justification();
            justification.Val = JustificationValues.Right;
            paragraphProperties.Append(justification);
        }

        paragraph.Append(paragraphProperties);
        paragraph.Append(CreateRun(text, bold, underline));
        body.Append(paragraph);
    }

    // Add a centered paragraph to the document

    static void AddCenteredParagraph(Body body, string text, bool bold, bool underline, bool keepWithNext = false)
    {
        Paragraph paragraph = new Paragraph();
        ParagraphProperties paragraphProperties = new ParagraphProperties();

        Justification justification = new Justification();
        justification.Val = JustificationValues.Center;

        SpacingBetweenLines spacing = new SpacingBetweenLines();
        spacing.After = "120";
        spacing.Line = "264";
        spacing.LineRule = LineSpacingRuleValues.Auto;

        // Keep a new legal heading together with the paragraph that follows it.
        if (keepWithNext)
        {
            paragraphProperties.Append(new KeepNext());
        }
        paragraphProperties.Append(spacing);
        paragraphProperties.Append(justification);
        paragraph.Append(paragraphProperties);
        paragraph.Append(CreateRun(text, bold, underline));
        body.Append(paragraph);
    }

    // Add one missing discovery item to the document

    static void AddMissingItem(Body body, int itemNumber, string? text)
    {
        Paragraph paragraph = new Paragraph();
        ParagraphProperties paragraphProperties = new ParagraphProperties();

        Indentation indentation = new Indentation();
        indentation.Left = "720";
        indentation.Hanging = "360";

        SpacingBetweenLines spacing = new SpacingBetweenLines();
        spacing.After = "80";
        spacing.Line = "264";
        spacing.LineRule = LineSpacingRuleValues.Auto;

        paragraphProperties.Append(spacing);
        paragraphProperties.Append(indentation);
        paragraph.Append(paragraphProperties);
        paragraph.Append(CreateRun(itemNumber + ". " + text, false, false));
        body.Append(paragraph);
    }

    // Create the text that will be placed inside a paragraph

    static Run CreateRun(string? text, bool bold, bool underline)
    {
        Run run = new Run();
        RunProperties runProperties = new RunProperties();

        RunFonts font = new RunFonts();
        font.Ascii = "Times New Roman";
        font.HighAnsi = "Times New Roman";

        FontSize fontSize = new FontSize();
        fontSize.Val = "24";

        runProperties.Append(font);

        if (bold == true)
        {
            runProperties.Append(new Bold());
        }

        runProperties.Append(fontSize);

        if (underline == true)
        {
            Underline underlineSetting = new Underline();
            underlineSetting.Val = UnderlineValues.Single;
            runProperties.Append(underlineSetting);
        }

        run.Append(runProperties);
        run.Append(new Text(new string((text ?? string.Empty).Where(c => XmlConvert.IsXmlChar(c)).ToArray())));

        return run;
    }

    // Set the document to letter size with one inch margins

    static void AddPageSettings(Body body)
    {
        SectionProperties section = new SectionProperties();

        PageSize pageSize = new PageSize();
        pageSize.Width = 12240;
        pageSize.Height = 15840;

        PageMargin pageMargin = new PageMargin();
        pageMargin.Top = 1440;
        pageMargin.Right = 1440;
        pageMargin.Bottom = 1440;
        pageMargin.Left = 1440;
        pageMargin.Header = 708;
        pageMargin.Footer = 708;
        pageMargin.Gutter = 0;

        section.Append(pageSize);
        section.Append(pageMargin);
        body.Append(section);
    }
}
