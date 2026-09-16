using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ResumeAnalyzerApp
{
    public class Resume
    {
        public string FullName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
        public decimal ExpectedSalary { get; set; }
        public string SourceFilePath { get; set; } = string.Empty;
    }

    public class ResumeAnalyzerService
    {
        public async Task<List<Resume>> LoadResumesAsync(IEnumerable<string> filePaths)
        {
            var loadTasks = filePaths.Select(async path =>
            {
                string content = await File.ReadAllTextAsync(path);
                return ParseResume(content, path);
            });

            Resume[] resumes = await Task.WhenAll(loadTasks);
            return resumes.ToList();
        }

        private Resume ParseResume(string fileContent, string filePath)
        {
            var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return new Resume
            {
                FullName = lines.ElementAtOrDefault(0) ?? "Unknown",
                City = lines.ElementAtOrDefault(1) ?? "Unknown",
                ExperienceYears = int.TryParse(lines.ElementAtOrDefault(2), out var exp) ? exp : 0,
                ExpectedSalary = decimal.TryParse(lines.ElementAtOrDefault(3), out var sal) ? sal : 0,
                SourceFilePath = filePath
            };
        }

        public Resume? GetMostExperienced(List<Resume> resumes) =>
            resumes.AsParallel().OrderByDescending(r => r.ExperienceYears).FirstOrDefault();

        public Resume? GetLeastExperienced(List<Resume> resumes) =>
            resumes.AsParallel().OrderBy(r => r.ExperienceYears).FirstOrDefault();

        public Resume? GetLowestSalary(List<Resume> resumes) =>
            resumes.AsParallel().OrderBy(r => r.ExpectedSalary).FirstOrDefault();

        public Resume? GetHighestSalary(List<Resume> resumes) =>
            resumes.AsParallel().OrderByDescending(r => r.ExpectedSalary).FirstOrDefault();

        public Dictionary<string, List<Resume>> GetCandidatesByCity(List<Resume> resumes) =>
            resumes.AsParallel()
                   .GroupBy(r => r.City)
                   .ToDictionary(g => g.Key, g => g.ToList());
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("=== Системне програмування: Аналізатор резюме ===");

            var analyzer = new ResumeAnalyzerService();

            string testDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResumes");
            Directory.CreateDirectory(testDir);

            File.WriteAllText(Path.Combine(testDir, "resume1.txt"), "Іван Петренко\nКиїв\n5\n1500");
            File.WriteAllText(Path.Combine(testDir, "resume2.txt"), "Марія Коваль\nЛьвів\n2\n800");
            File.WriteAllText(Path.Combine(testDir, "resume3.txt"), "Олексій Сидоренко\nКиїв\n10\n3000");

            var files = Directory.GetFiles(testDir, "*.txt");
            Console.WriteLine($"Завантаження {files.Length} резюме асинхронно...");

            var resumes = await analyzer.LoadResumesAsync(files);
            Console.WriteLine("Завантаження завершено успішно!\n");

            var mostExp = analyzer.GetMostExperienced(resumes);
            var leastExp = analyzer.GetLeastExperienced(resumes);
            var lowestSal = analyzer.GetLowestSalary(resumes);
            var highestSal = analyzer.GetHighestSalary(resumes);
            var byCity = analyzer.GetCandidatesByCity(resumes);

            Console.WriteLine($"[Звіт] Найдосвідченіший: {mostExp?.FullName} ({mostExp?.ExperienceYears} років досвіду)");
            Console.WriteLine($"[Звіт] Найменш досвідчений: {leastExp?.FullName} ({leastExp?.ExperienceYears} років досвіду)");
            Console.WriteLine($"[Звіт] Найнижча зарплата: {lowestSal?.FullName} (${lowestSal?.ExpectedSalary})");
            Console.WriteLine($"[Звіт] Найвища зарплата: {highestSal?.FullName} (${highestSal?.ExpectedSalary})");

            Console.WriteLine("\n[Звіт] Кандидати за містами:");
            foreach (var cityGroup in byCity)
            {
                Console.WriteLine($"  Місто: {cityGroup.Key}");
                foreach (var candidate in cityGroup.Value)
                {
                    Console.WriteLine($"    - {candidate.FullName}");
                }
            }

            Console.WriteLine("\nНатисніть будь-вашу клавішу для виходу...");
            Console.ReadKey();
        }
    }
}