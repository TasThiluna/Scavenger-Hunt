using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class scavengerHunt : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo bomb;

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
    public Texture tophalf;
    public Texture bottomhalf;
    public Texture lefthalf;
    public Texture righthalf;
    public Texture evencol;
    public Texture oddcol;
    public Texture evenrow;
    public Texture oddrow;
    public Transform animationPivot;
    public Transform statusLight;
    private static readonly string[] posnames = new string[16] { "A1", "B1", "C1", "D1", "A2", "B2", "C2", "D2", "A3", "B3", "C3", "D3", "A4", "B4", "C4", "D4" };
    private static readonly string[] colornames = new string[3] { "red", "green", "blue" };
    private static readonly string[][] mazes = new string[6][] { new string[16] { "dr", "lr", "lr", "l", "ud", "dr", "lr", "l", "ur", "uld", "dr", "l", "r", "ulr", "ulr", "l" },
                                                new string[16] { "dr", "dl", "d", "d", "u", "ur", "uld", "ud", "dr", "lrd", "ul", "ud", "u", "ur", "lr", "ul" },
                                                new string[16] { "r", "ldr", "ldr", "l", "rd", "ul", "ur", "dl", "ud", "r", "lr", "uld", "u", "r", "lr", "ul"  },
                                                new string[16] { "dr", "lr", "ldr", "l", "ur", "ld", "urd", "ld", "d", "ud", "ud", "ud", "ur", "ul", "u", "u"  },
                                                new string[16] { "dr", "dl", "dr", "l", "ud", "u", "ur", "dl", "ur", "dl", "r", "uld", "r", "ulr", "lr", "ul"  },
                                                new string[16] { "dr", "lr", "ld", "d", "ur", "dl", "ur", "ul", "dr", "ulr", "drl", "ld", "u", "r", "ul", "u"  } };

    private int mazeindex;
    private int colorindex;
    private int position;
    private int stage;
    private int keysquare;
    private int solutionsquare;
    private int[] symic = new int[16];
    private int[] reltiles = new int[2];
    private int[] decoytiles = new int[4];
    private int[] decoylocations = new int[2];

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in buttons)
            button.OnInteract += delegate () { buttonPress(button); return false; };
        submit.OnInteract += delegate () { PressSubmit(); return false; };
    }

    void Start()
    {
        var numbers = Enumerable.Range(0, 16).ToList();
        var decoycolornumbers = Enumerable.Range(0,3).ToList();
        if (bomb.GetBatteryCount() % 2 == 0) // Even number of batteries
          colorindex = 0;
        else if (bomb.GetSerialNumberLetters().Any(x => "AEIOU".Contains(x))) // SN contains a vowel
          colorindex = 1;
        else
          colorindex = 2;
        decoycolornumbers.Remove(colorindex);
        mazeindex = (bomb.GetSerialNumber()[5] - '0') % 6; // Last digit of SN mod 6
        position = rnd.Range(0, 16);
        keysquare = rnd.Range(0, 16); // The position that must be submitted in stage 1.
        solutionsquare = rnd.Range(0, 16); // The position that must be submitted in stage 2.
        for (int i = 0; i < 2; i++)
        {
            decoylocations[i] = numbers[rnd.Range(0, numbers.Count())];
            numbers.Remove(decoylocations[i]);
        }
        for (int i = 0; i < 16; i++)
            symic[i] = rnd.Range(0, 2);
        for (int i = 0; i < 2; i++)
        {
            reltiles[i] = numbers[rnd.Range(0, numbers.Count())];
            numbers.Remove(reltiles[i]);
            tiles[reltiles[i]].material = colors[colorindex];
        }
        for (int i = 0; i < 4; i++)
        {
            decoytiles[i] = numbers[rnd.Range(0, numbers.Count())];
            numbers.Remove(decoytiles[i]);
            tiles[decoytiles[i]].material = colors[(i == 0 || i == 1) ? decoycolornumbers[0] : decoycolornumbers[1]];
        }
        Debug.LogFormat("[Scavenger Hunt #{0}] You are in maze {1}.", moduleId, mazeindex);
        Debug.LogFormat("[Scavenger Hunt #{0}] You started in {1}.", moduleId, posnames[position]);
        Debug.LogFormat("[Scanvenger Hunt #{0}] The relevant color is {1}.", moduleId, colornames[colorindex]);
        Debug.LogFormat("[Scavenger Hunt #{0}] The relevant colored squares are at {1} and {2}.", moduleId, posnames[reltiles[0]], posnames[reltiles[1]]);
        Debug.LogFormat("[Scavenger Hunt #{0}] The decoy colored squares are at {1}, {2}, {3}, and {4}.", moduleId, posnames[decoytiles[0]], posnames[decoytiles[1]], posnames[decoytiles[2]], posnames[decoytiles[3]]);
        Debug.LogFormat("[Scavenger Hunt #{0}] The keysquare is at {1}.", moduleId, posnames[keysquare]);
        Debug.LogFormat("[Scavenger Hunt #{0}] The fake keysquares are at {1} and {2}.", moduleId, posnames[decoylocations[0]], posnames[decoylocations[1]]);
        tileState();
    }

    void stageTwo()
    {
        for (int i = 0; i < 16; i++)
            tiles[i].material = neutral;
        Debug.LogFormat("[Scavenger Hunt #{0}] The solution square is at {1}.", moduleId, posnames[solutionsquare]);
        tileState();
    }

    void tileState()
    {
        var allpositions = Enumerable.Range(0, 16).ToList();
        var unused = allpositions.Where(x => x != position).ToArray();
        for (int i = 0; i < unused.Count(); i++)
            tilesymbols[unused[i]].material.mainTexture = none;
        if (stage == 0)
        {
            if (!reltiles.Contains(position) && !decoytiles.Contains(position))
                tilesymbols[position].material.mainTexture = noclue;
            else if (position == reltiles[0])
                tilesymbols[position].material.mainTexture = columnsymbols[keysquare % 4];
            else if (position == reltiles[1])
                tilesymbols[position].material.mainTexture = rowsymbols[keysquare / 4];
            else if (position == decoytiles[0] || position == decoytiles[2])
                tilesymbols[position].material.mainTexture = columnsymbols[position == decoytiles[0] ? decoytiles[0] % 4 : decoytiles[2] % 4];
            else
                tilesymbols[position].material.mainTexture = rowsymbols[position == decoytiles[1] ? decoytiles[1] / 4 : decoytiles[3] / 4];
        }
        else
        {
            if (!decoytiles.Contains(position))
                tilesymbols[position].material.mainTexture = noclue;
            else if (position == decoytiles[0])
                tilesymbols[position].material.mainTexture = ((solutionsquare / 4 == 0 || solutionsquare / 4 == 1) ? tophalf : bottomhalf);
            else if (position == decoytiles[1])
                tilesymbols[position].material.mainTexture = ((solutionsquare % 4 == 0 || solutionsquare % 4 == 1) ? lefthalf : righthalf);
            else if (position == decoytiles[2])
                tilesymbols[position].material.mainTexture = ((solutionsquare / 4 == 0 || solutionsquare / 4 == 2) ? oddrow : evenrow);
            else
                tilesymbols[position].material.mainTexture = ((solutionsquare % 4 == 0 || solutionsquare % 4 == 2) ? oddcol : evencol);
        }
    }

    void buttonPress(KMSelectable button)
    {
        var ix = Array.IndexOf(buttons, button);
        button.AddInteractionPunch(.5f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        var directions = new int[] { -4, 1, 4, -1 };
        var markers = new char[] { 'u', 'r', 'd', 'l' };
        if (!mazes[mazeindex][position].Contains(markers[ix]))
        {
            Debug.LogFormat("[Scavenger Hunt #{0}] You ran into a wall. Strike!.", moduleId);
            GetComponent<KMBombModule>().HandleStrike();
        }
        else
        {
            position += directions[ix];
            tileState();
        }
    }

    void PressSubmit()
    {
        if (moduleSolved)
            return;
        submit.AddInteractionPunch(.5f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submit.transform);
        if (stage == 0)
        {
            if (position != keysquare)
            {
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[Scavenger Hunt #{0}] You submitted at {1}. That is not the keysquare. Strike!", moduleId, posnames[position]);
            }
            else
            {
                Debug.LogFormat("[Scavenger Hunt #{0}] You submitted at {1}. That is correct. Progressing to the next stage.", moduleId, posnames[position]);
                stage++;
                stageTwo();
            }
        }
        else if (position != solutionsquare)
        {
            GetComponent<KMBombModule>().HandleStrike();
            Debug.LogFormat("[Scavenger Hunt #{0}] You submitted at {1}. That is not the solution square. Strike!", moduleId, posnames[position]);
        }
        else
        {
            Debug.LogFormat("[Scavenger Hunt #{0}] You submitted at {1}. That is correct. Module solved.", moduleId, posnames[position]);
            moduleSolved = true;
            for (int i = 0; i < 4; i++)
              buttons[i].gameObject.SetActive(false);
            submit.gameObject.SetActive(false);
            for (int i = 0; i < 16; i++)
              tilesymbols[i].gameObject.SetActive(false);
            StartCoroutine(showStatusLight(tiles[solutionsquare].transform.localPosition));
            StartCoroutine(openFlap());
        }
    }

    private float easeInOutQuad(float time, float start, float end, float duration)
    {
        time /= duration / 2;
        if (time < 1)
            return (end - start) / 2 * time * time + start;
        time--;
        return -(end - start) / 2 * (time * (time - 2) - 1) + start;
    }

    private IEnumerator showStatusLight(Vector3 tilePosition)
    {
        var x = solutionsquare % 4;
        var y = solutionsquare / 4;
        statusLight.localPosition = new Vector3(tilePosition.x, -0.045f, tilePosition.z);

        var duration = 1.6f;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            statusLight.localPosition = new Vector3(tilePosition.x, easeInOutQuad(elapsed, -.045f, 0, duration), tilePosition.z);
            yield return null;
            elapsed += Time.deltaTime;
        }
        statusLight.localPosition = new Vector3(tilePosition.x, 0, tilePosition.z);

        GetComponent<KMBombModule>().HandlePass();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
    }

    private IEnumerator openFlap()
    {
        var x = solutionsquare % 4;
        var y = solutionsquare / 4;
        animationPivot.localPosition = new Vector3(.035f * x - 0.06875f, 0, 0);
        tiles[solutionsquare].transform.SetParent(animationPivot);

        var duration = .6f;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            animationPivot.localEulerAngles = new Vector3(0, 0, easeInOutQuad(elapsed, 0, 90, duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        animationPivot.localEulerAngles = new Vector3(0, 0, 90);
    }
}
