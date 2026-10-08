using System.Diagnostics;

namespace MotionGenerator
{
    public static class MotionLogic
    {
        // Collect the case details from the user.
        public static Case CollectCaseInfo()
        {
            Case caseInfo = new Case();

            caseInfo.DefendantName = AskQuestion("Defendant name: ");
            caseInfo.CaseNumber = AskQuestion("Case number: ");
            caseInfo.Court = AskQuestion("Court: ");
            caseInfo.AttorneyName = AskQuestion("Attorney name: ");
            caseInfo.Charge = AskQuestion("Charge: ");

            return caseInfo;
        }

        // Show the available discovery items.
        public static void DisplayDiscoveryOptions(List<DiscoveryItem> discoveryItems)
        {
            Console.WriteLine();
            Console.WriteLine("Which discovery was not provided?");

            for (int i = 0; i < discoveryItems.Count; i++)
            {
                Console.WriteLine((i + 1) + ". " + discoveryItems[i].Name);
            }
        }

        // Let the user select each missing discovery item.
        public static void SelectMissingDiscovery(List<DiscoveryItem> discoveryItems)
        {
            bool selectAnotherItem = true;

            while (selectAnotherItem == true)
            {
                int choice = AskForDiscoveryNumber(discoveryItems.Count);

                discoveryItems[choice - 1].IsMissing = true;

                Console.Write("Select another item? (Y/N): ");
                string? answer = Console.ReadLine();

                if (answer != null && answer.Trim().ToUpper() == "Y")
                {
                    selectAnotherItem = true;
                }
                else
                {
                    selectAnotherItem = false;
                }
            }
        }

        // Return the discovery items marked as missing.
        public static List<DiscoveryItem> GetMissingItems(List<DiscoveryItem> discoveryItems)
        {
            List<DiscoveryItem> missingItems = new List<DiscoveryItem>();

            for (int i = 0; i < discoveryItems.Count; i++)
            {
                if (discoveryItems[i].IsMissing == true)
                {
                    missingItems.Add(discoveryItems[i]);
                }
            }

            return missingItems;
        }

        // Build the output file path from the defendant's name.
        public static string BuildOutputPath(Case caseInfo)
        {
            string safeDefendantName = MakeSafeFileName(caseInfo.DefendantName);
            string fileName = safeDefendantName + "-Discovery-Motion.docx";
            string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string outputFolder = Path.Combine(documentsFolder, "CPL245Motions");
            string outputPath = Path.Combine(outputFolder, fileName);

            Directory.CreateDirectory(outputFolder);

            return outputPath;
        }

        // Ask a question until the user enters a value.
        public static string AskQuestion(string question)
        {
            string? answer = "";

            while (answer == "")
            {
                Console.Write(question);
                answer = Console.ReadLine();

                if (answer == null)
                {
                    answer = "";
                }

                answer = answer.Trim();

                if (answer == "")
                {
                    Console.WriteLine("Please enter a value.");
                }
            }

            return answer;
        }

        // Ask for a valid discovery item number.
        public static int AskForDiscoveryNumber(int numberOfItems)
        {
            int choice = 0;
            bool validChoice = false;

            while (validChoice == false)
            {
                Console.Write("Enter a number from 1 to " + numberOfItems + ": ");
                string? answer = Console.ReadLine();

                if (int.TryParse(answer, out choice) == true)
                {
                    if (choice >= 1 && choice <= numberOfItems)
                    {
                        validChoice = true;
                    }
                }

                if (validChoice == false)
                {
                    Console.WriteLine("That was not a valid choice. Please try again.");
                }
            }

            return choice;
        }

        // Replace characters that cannot be used in a file name.
        public static string MakeSafeFileName(string name)
        {
            char[] invalidCharacters = Path.GetInvalidFileNameChars();

            for (int i = 0; i < invalidCharacters.Length; i++)
            {
                name = name.Replace(invalidCharacters[i], '-');
            }

            name = name.Replace(' ', '-');

            return name;
        }

        // Open the finished document in Word.
        public static void OpenWordDocument(string filePath)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = filePath;
                startInfo.UseShellExecute = true;

                Process.Start(startInfo);

                Console.WriteLine("Opening the document...");
            }
            catch
            {
                Console.WriteLine("The document was saved, but it could not be opened automatically.");
                Console.WriteLine("Open the CPL245Motions folder inside your Documents folder.");
            }
        }
    }
}
