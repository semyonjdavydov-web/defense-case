using MotionGenerator;

class Program
{
    static void Main()
    {
        // Display the program name
        Console.WriteLine("CPL 245 Discovery Motion Generator");
        Console.WriteLine("----------------------------------");
        Console.WriteLine();

        // Collect case information from the user
        Case caseInfo = MotionLogic.CollectCaseInfo();

        // Load discovery options and prompt the user to select the missing items
        List<DiscoveryItem> discoveryItems = MockData.GetDiscoveryItems();
        MotionLogic.DisplayDiscoveryOptions(discoveryItems);
        MotionLogic.SelectMissingDiscovery(discoveryItems);

        // Build the list of missing discovery and the output path
        List<DiscoveryItem> missingItems = MotionLogic.GetMissingItems(discoveryItems);
        string outputPath = MotionLogic.BuildOutputPath(caseInfo);

        // Generate the motion template and exit the program
        TemplateGenerator generator = new TemplateGenerator();
        generator.CreateMotion(caseInfo, missingItems, outputPath);
        Console.WriteLine();
        Console.WriteLine("The Word document was created successfully:");
        Console.WriteLine(outputPath);
        MotionLogic.OpenWordDocument(outputPath);
        Console.WriteLine();
        Console.WriteLine("Press any key to close this window.");
        Console.ReadKey();
    }


}
