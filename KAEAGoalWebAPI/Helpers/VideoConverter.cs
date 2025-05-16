using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using KAEAGoalWebAPI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace KAEAGoalWebAPI.Helpers
{
    public class VideoConverter
    {
        public async Task<bool> ConvertHevcToMp4Async(string inputPath, string outputPath)
        {
            var ffmpegPath = "ffmpeg"; // หรือ path เต็ม เช่น @"C:\ffmpeg\bin\ffmpeg.exe"

            var arguments = $"-i \"{inputPath}\" -c:v libx264 -preset slow -crf 23 -c:a aac -strict -2 \"{outputPath}\"";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            string stderr = await process.StandardError.ReadToEndAsync(); // เก็บ error ไว้ debug
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
    }
}
