using System.Collections.Generic;

public static class DialogueParser
{
    public static DialogueLine[] Parse(string text)
    {
        List<DialogueLine> result = new List<DialogueLine>();
        string[] rows = text.Split('\n');

        foreach (string raw in rows)
        {
            string row = raw.Trim();
            if (row.Length == 0 || row.StartsWith("#")) continue;

            DialogueLine line = new DialogueLine();

            if (row.StartsWith("["))
            {
                int close = row.IndexOf(']');
                if (close > 0)
                {
                    line.speaker = row.Substring(1, close - 1).Trim();
                    row = row.Substring(close + 1).Trim();
                }
            }

            line.body = row.Replace("\\n", "\n");
            result.Add(line);
        }

        return result.ToArray();
    }
}