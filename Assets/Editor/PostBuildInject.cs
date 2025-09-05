// Assets/Editor/PostBuildInject.cs
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

public static class PostBuildInject
{
    // dipanggil setelah build selesai
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.WebGL) {
            Debug.Log("[PostBuildInject] not WebGL build. skipping.");
            return;
        }

        Debug.Log("[PostBuildInject] WebGL build finished at: " + pathToBuiltProject);

        // pathToBuiltProject biasanya folder WebGL Build (contains index.html)
        string indexPath = Path.Combine(pathToBuiltProject, "index.html");
        string bridgeSource = Path.Combine(Application.dataPath, "WebBridge/bridge.js"); // Assets/WebBridge/bridge.js
        string bridgeDest = Path.Combine(pathToBuiltProject, "bridge.js");

        if (!File.Exists(indexPath)) {
            Debug.LogError("[PostBuildInject] index.html not found at: " + indexPath);
            return;
        }
        if (!File.Exists(bridgeSource)) {
            Debug.LogError("[PostBuildInject] bridge.js not found at: " + bridgeSource);
            return;
        }

        // copy bridge.js into build folder
        File.Copy(bridgeSource, bridgeDest, true);
        Debug.Log("[PostBuildInject] Copied bridge.js to build folder.");

        // read index.html
        string html = File.ReadAllText(indexPath);

        // insert a script tag for bridge.js BEFORE Unity loader script or before </body>
        string insert = "<script src=\"bridge.js\"></script>\n";
        // Try find the loader script tag; if present insert before it
        int loaderIdx = html.IndexOf(".loader.js");
        if (loaderIdx >= 0)
        {
            // find beginning of the script tag containing loader
            int scriptStart = html.LastIndexOf("<script", loaderIdx);
            if (scriptStart >= 0)
            {
                // insert before that script
                html = html.Insert(scriptStart, insert);
                File.WriteAllText(indexPath, html);
                Debug.Log("[PostBuildInject] Injected bridge script before loader tag.");
                return;
            }
        }

        
        int bodyClose = html.IndexOf("</body>");
        if (bodyClose >= 0)
        {
            html = html.Insert(bodyClose, insert);
            File.WriteAllText(indexPath, html);
            Debug.Log("[PostBuildInject] Injected bridge script before </body> fallback.");
            return;
        }

        Debug.LogWarning("[PostBuildInject] Could not inject bridge script automatically (no loader tag nor </body> found).");
    }
}
