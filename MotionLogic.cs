using System.Diagnostics;

namespace MotionGenerator
{
    public static class MotionLogic
    {
        /// <summary>
        /// Collects the case information from the user and returns a Case object containing the information.
        /// </summary>
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

        /// <summary>
        /// Displays the list of discovery items to the user and prompts them to select which items are missing.
        /// </summary>
        /// <param name="discoveryItems"></param>
        public static void DisplayDiscoveryOptions(List<DiscoveryItem> discoveryItems)
        {
            Console.WriteLine();
            Console.WriteLine("Which discovery was not provided?");

            for (int i = 0; i < discoveryItems.Count; i++)
            {
                Console.WriteLine((i + 1) + ". " + discoveryItems[i].Name);
            }
        }

        /// <summary>
        /// Prompts the user to select which discovery items are missing and updates the IsMissing property of the selected items.
        /// </summary>
        /// <param name="discoveryItems"></param>
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

        /// <summary>
        /// Retrieves the list of discovery items that were selected as missing and returns them in a new list.
        /// </summary>
        /// <param name="discoveryItems"></param>
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

        /// <summary>
        /// Builds the output path for the Word document based on the case information and returns it as a string.
        /// </summary>
        /// <param name="caseInfo"></param>
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

        /// <summary>
        /// Asks the user a question and returns their answer as a string.
        /// </summary>
        /// <param name="question"></param>
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

        /// <summary>
        /// Asks the user to enter a number corresponding to a discovery item and returns the selected number.
        /// </summary>
        /// <param name="numberOfItems"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Removes invalid characters from a string to create a safe file name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Opens a Word document using the default program.
        /// </summary>
        /// <param name="filePath"></param>
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
