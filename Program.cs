using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageMagick;
using ImageMagick.Formats;

namespace FileLockingDemo
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting");


            var inFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppData");
            var outFolder1 = Path.Combine(inFolder, "MagickNetOutput");
            Directory.CreateDirectory(outFolder1);

            var outFolder2 = Path.Combine(inFolder, "MuPDFOutput");
            Directory.CreateDirectory(outFolder2);

            var outFolder3 = Path.Combine(inFolder, "CpdfOutput");
            Directory.CreateDirectory(outFolder3);

            var task1 = Task.Run(() =>
            {
                while (true)
                {
                    var muPdfOutFiles = MuPdf(outFolder2);
                    foreach (var file in muPdfOutFiles)
                    {
                        if (FileHelper.IsFileLocked(new FileInfo(file), out var whoIsLocking))
                        {
                            throw new Exception($"The file {file} created using MuPdf is locked by " + whoIsLocking.FirstOrDefault());
                        }
                        else
                        {
                            Console.WriteLine($"Not locked, good! {file}");
                        }
                    }
                }
            });

            var task2 = Task.Run(() =>
            {
                while (true)
                {
                    var outFile = Cpdf(outFolder3);
                    Console.WriteLine(outFile);
                    if (FileHelper.IsFileLocked(new FileInfo(outFile), out var whoIsLocking))
                    {
                        throw new Exception($"The file  {outFile} created using Cpdf is locked by " + whoIsLocking.FirstOrDefault());
                    }
                    else
                    {
                        Console.WriteLine($"Not locked, good! {outFile}");
                    }
                }
            });
            
            var task3 = Task.Run(() =>
            {
                while (true)
                {
                    var outFile = MagickNet(outFolder1);
                    Console.WriteLine(outFile);
                    if (FileHelper.IsFileLocked(new FileInfo(outFile), out var whoIsLocking))
                    {
                        throw new Exception($"The file  {outFile} created using MagickNet is locked by " + whoIsLocking.FirstOrDefault());
                    }
                    else
                    {
                        Console.WriteLine($"Not locked, good! {outFile}");
                    }
                }
            });

            Task.WaitAll(task1, task2, task3);
            Console.ReadLine();
        }

        private static string MagickNet(string outFolder)
        {
            var inTest1Image = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppData", "test1.jpg");
            var outTest1Pdf = Path.Combine(outFolder, "test1-output.pdf");

            var magickReadSettings = new MagickReadSettings();


            using (var image = new MagickImage(inTest1Image, magickReadSettings))
            {
                image.Settings.SetDefines(new PdfWriteDefines
                {
                    Author = "Image",
                    Producer = "Prod"
                });


                image.Write(outTest1Pdf, MagickFormat.Pdf);
            }

            return outTest1Pdf;
        }

        private static string[] MuPdf(string outFolder)
        {
            var exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mutool.exe");
            var inFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppData", "test2.pdf");
            var outFile = Path.Combine(outFolder, "result-%d.png");

            using (var p = new Process
            {
                StartInfo =
                {
                    FileName = exe,
                    Arguments = $"draw -o \"{outFile}\" \"{inFile}\" 1-200",
                    UseShellExecute = false
                }
            })
            {
                p.Start();
                p.WaitForExit();
            }

            return Directory.GetFiles(outFolder);
        }

        private static string Cpdf(string outFolder)
        {
            var exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cpdf.exe");
            var inFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppData", "test3.pdf");
            var outFile = Path.Combine(outFolder, "result.pdf");

            var p = new Process
            {
                StartInfo =
                {
                    FileName = exe,
                    Arguments = $"-debug-force -encrypt 128bit test test \"{inFile}\" -o \"{outFile}\"",
                    UseShellExecute = false
                }
            };
            {
                p.Start();
                p.WaitForExit();
            }

            return outFile;
        }
    }
}