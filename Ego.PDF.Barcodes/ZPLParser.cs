using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class ZplElement
{
    public string Content { get; set; }
    public string ReferencedTemplate { get; set; } // null si no referencia plantilla
}

public class ZplParser
{
    public Dictionary<string, string> Templates { get; private set; } = new();
    public List<ZplElement> ReferencingLabels { get; private set; } = new();
    //public List<string> StandaloneLabels { get; private set; } = new();

    public void Parse(string content)
    {
        var blocks = Regex.Matches(content, @"\^XA.*?\^XZ", RegexOptions.Singleline);

        foreach (Match block in blocks)
        {
            string zplBlock = block.Value;

            // ¿Es plantilla (^DF)?
            var dfMatch = Regex.Match(zplBlock, @"\^DF([^\^]+)");
            if (dfMatch.Success)
            {
                string templateName = dfMatch.Groups[ 1 ].Value.Trim();
                Templates[ templateName ] = zplBlock;
                continue;
            }

            // ¿Es etiqueta que referencia plantilla (^XF)?
            var xfMatch = Regex.Match(zplBlock, @"\^XF([^\^]+)");
            if (xfMatch.Success)
            {
                string referencedTemplate = xfMatch.Groups[ 1 ].Value.Trim();
                ReferencingLabels.Add(new ZplElement
                {
                    Content = zplBlock,
                    ReferencedTemplate = referencedTemplate
                });
                continue;
            }

            // Si no es ni ^DF ni ^XF → etiqueta independiente
            ReferencingLabels.Add(new ZplElement
            {
                Content = zplBlock,
                ReferencedTemplate = null
            });
            //StandaloneLabels.Add(zplBlock);
        }
    }

    public Dictionary<string, string> ExtractFieldValues(string labelContent)
    {
        var result = new Dictionary<string, string>();

        // Buscar pares ^FNn seguido de ^FD...^FS
        var matches = Regex.Matches(labelContent, @"\^FN(\d+)\^FD(.*?)\^FS");

        foreach (Match match in matches)
        {
            string fieldId = match.Groups[ 1 ].Value;
            string fieldValue = match.Groups[ 2 ].Value.Trim();
            result[ fieldId ] = fieldValue;
        }

        return result;
    }
}
