using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class scavengerHunt : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    public KMSelectable[] buttons;
    public KMSelectable submit;
    public Renderer[] tiles;
    public Renderer[] tilesymbols;
    public Material[] colors;
    public Material neutral;
    public Texture none;
    public Texture noclue;
    public Texture[] columnsymbols;
    public Texture[] rowsymbols;
    public Texture[] tophalf;
    public Texture[] bottomhalf;
    public Texture[] lefthalf;
    public Texture[] righthalf;
    public Texture[] evencol;
    public Texture[] oddcol;
    public Texture[] evenrow;
    public Texture[] oddrow;
    public Transform animationPivot;
    public Transform statusLight;

    private static readonly string[] posNames = new string[16] { "A1", "B1", "C1", "D1", "A2", "B2", "C2", "D2", "A3", "B3", "C3", "D3", "A4", "B4", "C4", "D4" };
    private static readonly string[] colorNames = new string[3] { "red", "green", "blue" };
    private static readonly string[][] mazes = new string[6][]
    {
        new string[16] { "dr", "lr", "lr", "l", "ud", "dr", "lr", "l", "ur", "uld", "dr", "l", "r", "ulr", "ulr", "l" },
        new string[16] { "dr", "dl", "d", "d", "u", "ur", "uld", "ud", "dr", "lrd", "ul", "ud", "u", "ur", "lr", "ul" },
        new string[16] { "r", "ldr", "ldr", "l", "rd", "ul", "ur", "dl", "ud", "r", "lr", "uld", "u", "r", "lr", "ul"  },
        new string[16] { "dr", "lr", "ldr", "l", "ur", "ld", "urd", "ld", "d", "ud", "ud", "ud", "ur", "ul", "u", "u"  },
        new string[16] { "dr", "dl", "dr", "l", "ud", "u", "ur", "dl", "ur", "dl", "r", "uld", "r", "ulr", "lr", "ul"  },
        new string[16] { "dr", "lr", "ld", "d", "ur", "dl", "ur", "ul", "dr", "ulr", "drl", "ld", "u", "r", "ul", "u"  }
     };

    private int mazeIndex;
    private int colorIndex;
    private int position;
    private int stage;
    private int keySquare;
    private int solutionSquare;
    private int[] relTiles = new int[2];
    private int[] decoyTiles = new int[4];
    private int[] decoyLocations = new int[2];
    private int[] symbolIndices = new int[16];

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in buttons)
            button.OnInteract += delegate () { ButtonPress(button); return false; };
        submit.OnInteract += delegate () { PressSubmit(); return false; };
    }

    void Start()
    {
        statusLight.gameObject.SetActive(false);
        var numbers = Enumerable.Range(0, 16).ToList();
        var decoyColorNumbers = Enumerable.Range(0, 3).ToList();
        if (bomb.GetBatteryCount() % 2 == 0)
            colorIndex = 0;
        else if (bomb.GetSerialNumberLetters().Any(x => "AEIOU".Contains(x)))
            colorIndex = 1;
        else
            colorIndex = 2;
        decoyColorNumbers.Remove(colorIndex);
        mazeIndex = (bomb.GetSerialNumber()[5] - '0') % 6;
        position = rnd.Range(0, 16);
        keySquare = rnd.Range(0, 16);
        solutionSquare = rnd.Range(0, 16);
        for (int i = 0; i < 2; i++)
        {
            decoyLocations[i] = numbers[rnd.Range(0, numbers.Count())];
            numbers.Remove(decoyLocations[i]);
        }
        for (int i = 0; i < 16; i++)
            symbolIndices[i] = rnd.Range(0, 4);
        for (int i = 0; i < 2; i++)
        {
            relTiles[i] = numbers[rnd.Range(0, numbers.Count())];
            numbers.Remove(relTiles[i]);
            tiles[relTiles[i]].material = colors[colorIndex];
        }
        for (int i = 0; i < 4; i++)
        {
            decoyTiles[i] = numbers[rnd.Range(0, numbers.Count())];
            numbers.Remove(decoyTiles[i]);
            tiles[decoyTiles[i]].material = colors[(i == 0 || i == 1) ? decoyColorNumbers[0] : decoyColorNumbers[1]];
        }
        Debug.LogFormat("[Scavenger Hunt #{0}] You are in maze {1}.", moduleId, mazeIndex);
        Debug.LogFormat("[Scavenger Hunt #{0}] You started in {1}.", moduleId, posNames[position]);
        Debug.LogFormat("[Scavenger Hunt #{0}] The relevant color is {1}.", moduleId, colorNames[colorIndex]);
        Debug.LogFormat("[Scavenger Hunt #{0}] The relevant colored squares are at {1} and {2}.", moduleId, posNames[relTiles[0]], posNames[relTiles[1]]);
        Debug.LogFormat("[Scavenger Hunt #{0}] The decoy colored squares are at {1}, {2}, {3}, and {4}.", moduleId, posNames[decoyTiles[0]], posNames[decoyTiles[1]], posNames[decoyTiles[2]], posNames[decoyTiles[3]]);
        Debug.LogFormat("[Scavenger Hunt #{0}] The solution square for stage 1 is at {1}.", moduleId, posNames[keySquare]);
        TileState();
    }

    void StageTwo()
    {
        for (int i = 0; i < 16; i++)
        {
            tiles[i].material = neutral;
            symbolIndices[i] = rnd.Range(0, 4);
        }
        Debug.LogFormat("[Scavenger Hunt #{0}] The solution square for stage 2 is at {1}.", moduleId, posNames[solutionSquare]);
        TileState();
    }

    void TileState()
    {
        var allPositions = Enumerable.Range(0, 16).ToList();
        var unused = allPositions.Where(x => x != position).ToArray();
        for (int i = 0; i < unused.Count(); i++)
            tilesymbols[unused[i]].material.mainTexture = none;
        if (stage == 0)
        {
            if (!relTiles.Contains(position) && !decoyTiles.Contains(position))
                tilesymbols[position].material.mainTexture = noclue;
            else if (position == relTiles[0])
                tilesymbols[position].material.mainTexture = columnsymbols[(keySquare % 4) * 4 + symbolIndices[position]];
            else if (position == relTiles[1])
                tilesymbols[position].material.mainTexture = rowsymbols[(keySquare / 4) * 4 + symbolIndices[position]];
            else if (position == decoyTiles[0] || position == decoyTiles[2])
                tilesymbols[position].material.mainTexture = columnsymbols[(position == decoyTiles[0] ? decoyTiles[0] % 4 : decoyTiles[2] % 4) * 4 + symbolIndices[position]];
            else
                tilesymbols[position].material.mainTexture = rowsymbols[(position == decoyTiles[1] ? decoyTiles[1] / 4 : decoyTiles[3] / 4) * 4 + symbolIndices[position]];
        }
        else
        {
            if (!decoyTiles.Contains(position))
                tilesymbols[position].material.mainTexture = noclue;
            else if (position == decoyTiles[0])
                tilesymbols[position].material.mainTexture = ((solutionSquare / 4 == 0 || solutionSquare / 4 == 1) ? tophalf[symbolIndices[position]] : bottomhalf[symbolIndices[position]]);
            else if (position == decoyTiles[1])
                tilesymbols[position].material.mainTexture = ((solutionSquare % 4 == 0 || solutionSquare % 4 == 1) ? lefthalf[symbolIndices[position]] : righthalf[symbolIndices[position]]);
            else if (position == decoyTiles[2])
                tilesymbols[position].material.mainTexture = ((solutionSquare / 4 == 0 || solutionSquare / 4 == 2) ? oddrow[symbolIndices[position]] : evenrow[symbolIndices[position]]);
            else
                tilesymbols[position].material.mainTexture = ((solutionSquare % 4 == 0 || solutionSquare % 4 == 2) ? oddcol[symbolIndices[position]] : evencol[symbolIndices[position]]);
        }
    }

    void ButtonPress(KMSelectable button)
    {
        var ix = Array.IndexOf(buttons, button);
        button.AddInteractionPunch(.5f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        var directions = new int[] { -4, 1, 4, -1 };
        var markers = new char[] { 'u', 'r', 'd', 'l' };
        if (!mazes[mazeIndex][position].Contains(markers[ix]))
        {
            Debug.LogFormat("[Scavenger Hunt #{0}] You ran into a wall. Strike!.", moduleId);
            module.HandleStrike();
        }
        else
        {
            position += directions[ix];
            TileState();
        }
    }

    void PressSubmit()
    {
        if (moduleSolved)
            return;
        submit.AddInteractionPunch(.5f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submit.transform);
        if (stage == 0)
        {
            if (position != keySquare)
            {
                module.HandleStrike();
                Debug.LogFormat("[Scavenger Hunt #{0}] You submitted at {1}. That is not the solution square for stage 1. Strike!", moduleId, posNames[position]);
            }
            else
            {
                Debug.LogFormat("[Scavenger Hunt #{0}] You submitted at {1}. That is correct. Progressing to the next stage.", moduleId, posNames[position]);
                stage++;
                StageTwo();
            }
        }
        else if (position != solutionSquare)
        {
            module.HandleStrike();
            Debug.LogFormat("[Scavenger Hunt #{0}] You submitted at {1}. That is not the solution square for stage 2. Strike!", moduleId, posNames[position]);
        }
        else
        {
            Debug.LogFormat("[Scavenger Hunt #{0}] You submitted at {1}. That is correct. Module solved.", moduleId, posNames[position]);
            for (int i = 0; i < 4; i++)
                buttons[i].gameObject.SetActive(false);
            submit.gameObject.SetActive(false);
            for (int i = 0; i < 16; i++)
                tilesymbols[i].gameObject.SetActive(false);
            StartCoroutine(ShowStatusLight(tiles[solutionSquare].transform.localPosition));
            StartCoroutine(OpenFlap());
        }
    }

    private float EaseInOutQuad(float time, float start, float end, float duration)
    {
        time /= duration / 2;
        if (time < 1)
            return (end - start) / 2 * time * time + start;
        time--;
        return -(end - start) / 2 * (time * (time - 2) - 1) + start;
    }

    private IEnumerator ShowStatusLight(Vector3 tilePosition)
    {
        statusLight.localPosition = new Vector3(tilePosition.x, -0.045f, tilePosition.z);
        statusLight.gameObject.SetActive(true);

        var duration = 1.6f;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            statusLight.localPosition = new Vector3(tilePosition.x, EaseInOutQuad(elapsed, -.045f, 0, duration), tilePosition.z);
            yield return null;
            elapsed += Time.deltaTime;
        }
        statusLight.localPosition = new Vector3(tilePosition.x, 0, tilePosition.z);

        module.HandlePass();
        moduleSolved = true;
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
    }

    private IEnumerator OpenFlap()
    {
        var x = solutionSquare % 4;
        animationPivot.localPosition = new Vector3(.035f * x - 0.06875f, 0, 0);
        tiles[solutionSquare].transform.SetParent(animationPivot);

        var duration = .6f;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            animationPivot.localEulerAngles = new Vector3(0, 0, EaseInOutQuad(elapsed, 0, 90, duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        animationPivot.localEulerAngles = new Vector3(0, 0, 90);
    }

    // Twitch Plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} move u/d/l/r [Moves the specified direction in the maze] | !{0} submit [Submits the current position] | !{0} reset [Resets the module back to stage 1] | Moves can be chained, for example '!{0} move uuddlrl'";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*reset\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (stage == 0)
                yield return "sendtochaterror Module cannot be reset to stage 1 when it is already on stage 1!";
            else
            {
                stage = 0;
                var decoyColorNumbers = Enumerable.Range(0, 3).ToList();
                if (bomb.GetBatteryCount() % 2 == 0)
                    colorIndex = 0;
                else if (bomb.GetSerialNumberLetters().Any(x => "AEIOU".Contains(x)))
                    colorIndex = 1;
                else
                    colorIndex = 2;
                decoyColorNumbers.Remove(colorIndex);
                for (int i = 0; i < 2; i++)
                    tiles[relTiles[i]].material = colors[colorIndex];
                for (int i = 0; i < 4; i++)
                    tiles[decoyTiles[i]].material = colors[(i == 0 || i == 1) ? decoyColorNumbers[0] : decoyColorNumbers[1]];
                TileState();
                Debug.LogFormat("[Scavenger Hunt #{0}] Module reset back to stage 1! (TP)", moduleId);
            }
            yield break;
        }
        if (Regex.IsMatch(command, @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            submit.OnInteract();
            yield break;
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*move\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length >= 2)
            {
                string checks = "";
                for (int j = 1; j < parameters.Length; j++)
                    checks += parameters[j];
                var buttonsToPress = new List<KMSelectable>();
                for (int i = 0; i < checks.Length; i++)
                {
                    if (checks.ElementAt(i).Equals('u') || checks.ElementAt(i).Equals('U'))
                        buttonsToPress.Add(buttons[0]);
                    else if (checks.ElementAt(i).Equals('d') || checks.ElementAt(i).Equals('D'))
                        buttonsToPress.Add(buttons[2]);
                    else if (checks.ElementAt(i).Equals('l') || checks.ElementAt(i).Equals('L'))
                        buttonsToPress.Add(buttons[3]);
                    else if (checks.ElementAt(i).Equals('r') || checks.ElementAt(i).Equals('R'))
                        buttonsToPress.Add(buttons[1]);
                    else
                    {
                        yield return "sendtochaterror Invalid movement character detected: '" + checks.ElementAt(i) + "'";
                        yield break;
                    }
                }
                yield return null;
                var ix = 0;
                var markers = new char[] { 'u', 'r', 'd', 'l' };
                for (int i = 0; i < buttonsToPress.Count; i++)
                {
                    ix = Array.IndexOf(buttons, buttonsToPress[i]);
                    if (!mazes[mazeIndex][position].Contains(markers[ix]))
                        yield return "strike";
                    buttonsToPress[i].OnInteract();
                    yield return new WaitForSeconds(0.15f);
                }
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        for (int i = 0; i < 2; i++)
        {
            var target = i == 0 ? keySquare : solutionSquare;
            var q = new Queue<int>();
            var allMoves = new List<Movement>();
            q.Enqueue(position);
            while (q.Count > 0)
            {
                var next = q.Dequeue();
                if (next == target)
                    goto readyToSubmit;
                var cell = mazes[mazeIndex][next];
                var allDirections = "urdl";
                var offsets = new int[] { -4, 1, 4, -1 };
                for (int j = 0; j < 4; j++)
                {
                    if (cell.Contains(allDirections[j]) && !allMoves.Any(x => x.start == next + offsets[j]))
                    {
                        q.Enqueue(next + offsets[j]);
                        allMoves.Add(new Movement { start = next, end = next + offsets[j], direction = j });
                    }
                }
            }
            throw new InvalidOperationException("There is a bug in maze generation.");
            readyToSubmit:
            if (allMoves.Count != 0) // Checks for position already being target
            {
                var lastMove = allMoves.First(x => x.end == target);
                var relevantMoves = new List<Movement> { lastMove };
                while (lastMove.start != position)
                {
                    lastMove = allMoves.First(x => x.end == lastMove.start);
                    relevantMoves.Add(lastMove);
                }
                for (int j = 0; j < relevantMoves.Count; j++)
                {
                    buttons[relevantMoves[relevantMoves.Count - 1 - j].direction].OnInteract();
                    yield return new WaitForSeconds(.1f);
                }
            }
            submit.OnInteract();
            if (i == 0)
                yield return new WaitForSeconds(.2f);
        }
        while (!moduleSolved)
        {
            yield return true;
            yield return new WaitForSeconds(.1f);
        }
    }

    class Movement
    {
        public int start;
        public int end;
        public int direction;
    }

}
