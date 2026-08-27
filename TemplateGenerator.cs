using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MotionGenerator;

class TemplateGenerator
{

    /// <summary>
    /// What the function does
    /// </summary>
    /// <param name="the parameters. If there are multiple parameters, list them with each enclosing signs"></param>
    /// <returns> Only if there is something the function returns </returns>
    public void CreateMotion(Case caseInfo, List<DiscoveryItem> missingItems, string filePath)
    {
        // Create a new Word document

        using (WordprocessingDocument document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();

            Body body = new Body();
            mainPart.Document.Append(body);

            // Add the court caption to the document
            AddCenteredParagraph(body, caseInfo.Court.ToUpper(), true, false);
            AddCenteredParagraph(body, "STATE OF NEW YORK", false, false);
            AddCenteredParagraph(body, "------------------------------------------------------------X", false, false);
            AddParagraph(body, "THE PEOPLE OF THE STATE OF NEW YORK,", false, false, false);
            AddCenteredParagraph(body, "-against-", false, false);
            AddParagraph(body, caseInfo.DefendantName.ToUpper() + ",", false, false, false);
            AddParagraph(body, "Defendant.", false, false, true);
            AddCenteredParagraph(body, "------------------------------------------------------------X", false, false);

            // Add the name of the motion
            AddCenteredParagraph(body, "NOTICE OF MOTION TO DISMISS", true, true);
            AddCenteredParagraph(body, "FOR FAILURE TO PROVIDE DISCOVERY", true, true);

            // Add the case information
            AddParagraph(body, "Case Number: " + caseInfo.CaseNumber, true, false, false);
            AddParagraph(body, "Charge: " + caseInfo.Charge, true, false, false);

            // Add the opening paragraph and the list of missing discovery items to the document
            string opening = "PLEASE TAKE NOTICE that the defendant, " + caseInfo.DefendantName;
            opening = opening + ", by attorney " + caseInfo.AttorneyName;
            opening = opening + ", respectfully moves this Court for dismissal based on the prosecution's failure ";
            opening = opening + "to provide required discovery within the time allowed by CPL Article 245.";
            AddParagraph(body, opening, false, false, false);
            AddParagraph(body, "The following discovery remains outstanding:", true, false, false);

            // Add each missing discovery item to the document
            for (int i = 0; i < missingItems.Count; i++)
            {
                AddMissingItem(body, i + 1, missingItems[i].Name);
            }
            string request = "Because these materials have not been provided, the defense respectfully requests ";
            request = request + "that the Court dismiss the case, or grant any other relief the Court finds appropriate.";
            AddParagraph(body, request, false, false, false);
            AddParagraph(body, "Dated: " + DateTime.Today.ToString("MMMM d, yyyy"), false, false, false);
            AddParagraph(body, "Respectfully submitted,", false, false, false);
            AddParagraph(body, caseInfo.AttorneyName, true, false, false);

            // Add the page size and margins to the end of the document
            AddPageSettings(body);

            // Save the finished Word document
            mainPart.Document.Save();
        }
    }

    // Add a regular paragraph to the document

    static void AddParagraph(Body body, string text, bool bold, bool underline, bool rightAligned)
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

    static void AddCenteredParagraph(Body body, string text, bool bold, bool underline)
    {
        Paragraph paragraph = new Paragraph();
        ParagraphProperties paragraphProperties = new ParagraphProperties();

        Justification justification = new Justification();
        justification.Val = JustificationValues.Center;

        SpacingBetweenLines spacing = new SpacingBetweenLines();
        spacing.After = "120";
        spacing.Line = "264";
        spacing.LineRule = LineSpacingRuleValues.Auto;

        paragraphProperties.Append(justification);
        paragraphProperties.Append(spacing);
        paragraph.Append(paragraphProperties);
        paragraph.Append(CreateRun(text, bold, underline));
        body.Append(paragraph);
    }

    // Add one missing discovery item to the document

    static void AddMissingItem(Body body, int itemNumber, string text)
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

        paragraphProperties.Append(indentation);
        paragraphProperties.Append(spacing);
        paragraph.Append(paragraphProperties);
        paragraph.Append(CreateRun(itemNumber + ". " + text, false, false));
        body.Append(paragraph);
    }

    // Create the text that will be placed inside a paragraph

    static Run CreateRun(string text, bool bold, bool underline)
    {
        Run run = new Run();
        RunProperties runProperties = new RunProperties();

        RunFonts font = new RunFonts();
        font.Ascii = "Times New Roman";
        font.HighAnsi = "Times New Roman";

        FontSize fontSize = new FontSize();
        fontSize.Val = "24";

        runProperties.Append(font);
        runProperties.Append(fontSize);

        if (bold == true)
        {
            runProperties.Append(new Bold());
        }

        if (underline == true)
        {
            Underline underlineSetting = new Underline();
            underlineSetting.Val = UnderlineValues.Single;
            runProperties.Append(underlineSetting);
        }

        run.Append(runProperties);
        run.Append(new Text(text));

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
