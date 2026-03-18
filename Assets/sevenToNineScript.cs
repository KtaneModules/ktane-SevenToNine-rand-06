using System.Collections;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class sevenToNineScript : MonoBehaviour {

	public KMSelectable yesButton, noButton;
	public TextMesh numberText, stageText;
	public KMAudio Audio;
	
	private int stage = -1;
	private int[] stageNumbers = new int[3];
	private int[] answers = new int[3];
	private int pressedYes = 0;

	private readonly string[] indicators =
	{
		"", "|                  |", "||                ||"
	};

	public MeshRenderer module;
	public GameObject buttonText;
	public GameObject anotherButtonText;
	
	static int ModuleIdCounter = 1;
	int ModuleId;
	private bool moduleSolved;
	
	int getRandomNumber(int amountofDigits)
	{
		int upperLimit = 1;
		for (int i = 0; i < amountofDigits; i++) upperLimit *= 10;
		upperLimit /= 9;
		int ans = Random.Range(upperLimit/10, upperLimit)+1;
		return ans * 9;
	}

	IEnumerator cycle()
	{
		float hue = 1 / 3f;
		while (true)
		{
			hue += 1 / 360f;
			if (hue > 1f) hue -= 1f;
			yield return new  WaitForSeconds(1/60f);
			module.material.color = Color.HSVToRGB(hue, .4f, .8f);
		}
	}

	IEnumerator rise()
	{
		float timer = 0f;
		while (timer <= 1f)
		{
			timer += 0.02f;
			yield return new WaitForSeconds(0.02f);
			module.material.color = Color.Lerp(
				Color.HSVToRGB(1/3f,0,1f),
				Color.HSVToRGB(1/3f,.4f,0.8f),
				timer
				);
		}

		yield return cycle();
	}

	void solve()
	{
		stageText.text = "|||              |||";
		moduleSolved = true;
		numberText.text = "SOLVED!";
		GetComponent<KMBombModule>().HandlePass();
		buttonText.transform.localPosition = anotherButtonText.transform.localPosition;
		buttonText.GetComponent<TextMesh>().text = "☺";
		Audio.HandlePlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
		StartCoroutine(rise());
	}
	
	void proceedStage()
	{
		stage++;
		if (stage == 3)
		{
			solve();
			return;
		}
		stageText.text = indicators[stage];
		numberText.text = stageNumbers[stage].ToString();
	}

	void press(bool yes)
	{
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		if (yes ^ answers[stage]!=0) {GetComponent<KMBombModule>().HandleStrike();
			return;
		}

		if (!yes && answers[stage] == 0)
		{
			proceedStage();
			return;
		}

		if (yes && answers[stage] != 0)
		{
			if (pressedYes==0) StartCoroutine(timer());
			pressedYes++;
		}
		
	}

	IEnumerator timer()
	{
		float timer = 0f;
		while (timer < 1f)
		{
			yield return new WaitForSeconds(0.05f);
			timer += 0.05f;
		}
		if (pressedYes == answers[stage]) proceedStage();
		else GetComponent<KMBombModule>().HandleStrike();
	}
	
	void Awake() { ModuleId = ModuleIdCounter++; }
	
	void Start () {
		module.material.color = Color.white;
		stageNumbers = Enumerable.Range(0,3).Select(x=>getRandomNumber(x+7)).ToArray();
		answers = stageNumbers.Select(x=>x%729==0?2:x%27==0?1:0).ToArray();
		Debug.LogFormat(@"[Seven to Nine #{0}] Numbers are: 
[Seven to Nine #{0}] {1} ({2}divisible by {3}),
[Seven to Nine #{0}] {4} ({5}divisible by {6}),
[Seven to Nine #{0}] {7} ({8}divisible by {9}),", ModuleId, 
			stageNumbers[0], answers[0]==0?"not ":"", answers[0]==2?"729":"27",
			stageNumbers[1], answers[1]==0?"not ":"", answers[1]==2?"729":"27",
			stageNumbers[2], answers[2]==0?"not ":"", answers[2]==2?"729":"27"
			);
		proceedStage();
		yesButton.OnInteract += delegate
		{
			if (!moduleSolved) press(true);
			return false;
		};
		noButton.OnInteract += delegate
		{
			if (!moduleSolved) press(false);
			return false;
		};
	}
	
#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"Use !{0} :( to press :( button. Use !{0} :) to press :) button. Use !{0} :)) to press :) button twice.";
#pragma warning restore 414
	
	public IEnumerator ProcessTwitchCommand(string Command)
	{
		yield return null;
		switch (Command)
		{
			case ":(": press(false); break;
			case ":)": press(true); break;
			case ":))": press(true); press(true); break;
			default: yield return "sendtochaterror Invalid command."; yield break;
		}
	}

	public IEnumerator TwitchHandleForcedSolve()
	{
		string[] commands = { ":(", ":)", ":))" };
		while (!moduleSolved)
		{
			int oldStage = stage;
			ProcessTwitchCommand(commands[answers[stage]]);
			yield return new WaitWhile(() => stage == oldStage);
		}
	}
}
