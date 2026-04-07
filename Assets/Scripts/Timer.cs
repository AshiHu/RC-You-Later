using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

/* Script ou l'on ajoute le système de sauvegarde qui saauvegarde le temps et le meilleur score, il permet aussi de 
de changer le fomat de sauvegarde en base64 pour éviter les changements manuels du fichier de sauvegarde. */

public static class Timer
{
    private static readonly Stopwatch stopwatch = new();
    private static List<long> steps = new();

    // La sauvegarde se fait dans dans le dossier persistant de l'application, avec un nom de fichier score.txt
    private static readonly string savePath = Path.Combine(
        UnityEngine.Application.dataPath, "..", "score.txt"
    );

    public static bool IsRunning
    {
        get => stopwatch.IsRunning;
    }

    public static double ElapsedSeconds
    {
        get => stopwatch.ElapsedMilliseconds * 0.001f;
    }

    public static int StepsCount
    {
        get => steps.Count;
    }

    public static double GetStepElapsedSeconds(int index)
    {
        return steps[index] * 0.001f;
    }

    public static void Reset()
    {
        stopwatch.Reset();
        steps.Clear();
    }

    public static void Start()
    {
        stopwatch.Start();
    }

    public static void Stop()
    {
        stopwatch.Stop();
    }

    public static void Step()
    {
        steps.Add(stopwatch.ElapsedMilliseconds);
    }

    public static void Save()
    {
        if (steps.Count == 0) return;

        // Le temps final = dernier step
        long newFinalTime = steps[steps.Count - 1];

        // Création d'une sauvegarde de meilleur temps : si un fichier existe déjà, on le lit et on compare le temps final
        if (File.Exists(savePath))
        {
            List<long> existingSteps = LoadStepsFromFile();
            if (existingSteps != null && existingSteps.Count > 0)
            {
                long oldFinalTime = existingSteps[existingSteps.Count - 1];
                if (newFinalTime >= oldFinalTime)
                {
                    // Pas un meilleur temps, on ne sauvegarde pas
                    return;
                }
            }
        }

        // Construire la chaîne : une valeur par ligne
        StringBuilder sb = new StringBuilder();
        foreach (long step in steps)
        {
            sb.AppendLine(step.ToString());
        }

        // Mis en place du système de sauvegarde en base64 pour éviter les changement manuels du fichier de sauvegarde
        string plainText = sb.ToString();
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));

        File.WriteAllText(savePath, encoded);
    }

        // elle appelle LoadStepsFromFile() et si le résultat n'est pas null, elle remplace steps par les valeurs chargées
    public static void Load()
    {
        List<long> loaded = LoadStepsFromFile();
        if (loaded != null)
        {
            steps = loaded;
        }
    }

    // Méthode privée partagée par Save et Load
    private static List<long> LoadStepsFromFile()
    {
        if (!File.Exists(savePath)) return null;

        try
        {
            string encoded = File.ReadAllText(savePath);

            // Décodage depuis fichier qui est en Base64
            string plainText = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));

            List<long> loaded = new List<long>();
            string[] lines = plainText.Split(
                new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries
            );

            foreach (string line in lines)
            {
                if (long.TryParse(line, out long value))
                {
                    loaded.Add(value);
                }
            }

            return loaded.Count > 0 ? loaded : null;
        }
        catch
        {
            // Fichier corrompu ou invalide
            return null;
        }
    }
}