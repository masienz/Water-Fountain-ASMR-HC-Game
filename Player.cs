using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;
using PathCreation;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.UI;
using MoreMountains.NiceVibrations;
using System.Net.PeerToPeer.Collaboration;
using SplineMesh;
using static Unity.VisualScripting.Member;

public class Player : MonoBehaviour
{
	[System.Serializable]
	public struct FountainSet { public List<Fountain> fountains; public List<GameObject> otherObjs; }

	[System.Serializable]
	public struct Fountain
	{
		public ContortAlong contortAlong;
		public GameObject flowObj;
		public GameObject flowGroundObj;
		public GameObject moneyTextEffectPos;
		public GameObject pipe;
		public GameObject pipeInnerObj;
		public float pipeOnShaderStartValue;
		public float pipeOnShaderEndValue;
		public float pipeOnShaderTransitionSeconds;
		public List<GameObject> objects;
	}

	public List<FountainSet> fountainSets;
	public Transform camera;

	[Header("Rotate")]
	public GameObject rotatingObj;
	public Rigidbody rb;
	[SerializeField] private float rotateSpeed = 10;
	[SerializeField] private Transform openPos;
	[SerializeField] private Transform closePos;

	[HideInInspector] public int openFountainCount;
	private FountainSet currentFountainSet;
	private FountainSet lastFountainSet;

	[Header("Stats")]
	public float money = 0;
	public int fountainLevel = 1;
	public int speedLevel = 1;
	public int incomeLevel = 1;
	public float moneyIncomeForEachFlow = 10;

	[Header("Speed")]
	public float fastSpeedFactorOnTouch = 1.5f;
	public float flowWaitTime = 1.0f;

	[Header("Level Max Counts")]
	public int maxSpeedLevel = 7;
	public int maxIncomeLevel = 7;

	[Header("Level Start Counts")]
	public float speedStartCount = 1f;
	public float incomeStartCount = 1f;

	[Header("Level Increase Counts")]
	public float speedIncreaseByLevel = 0.05f;
	public float incomeIncreaseByLevel = 0.5f;

	[Header("Level Money Counts")]
	public float fountainMoneyByLevel = 50;
	public float speedMoneyByLevel = 50;
	public float incomeMoneyByLevel = 50;

	//Current Level Counts
	private float currentMoneyIncrease = 1;
	private float currentSpeedNormal = 1;
	private float currentSpeed = 1;

	//Current Money Counts
	private float currentFountainLevelMoney = 50;
	private float currentSpeedLevelMoney = 50;
	private float currentIncomeLevelMoney = 50;

	[Header("Shader On Settings")]
	public Material waterfallOnMat;
	public float shaderOnStartValue = 0;
	public float shaderOnEndValue = 0;
	public float shaderOnTransitionSeconds = 1f;

	[Header("Shader Off Settings")]
	public Material waterfallOffMat;
	public float shaderOffStartValue = 0;
	public float shaderOffEndValue = 0;
	public float shaderOffTransitionSeconds = 1f;

	[Header("Effect")]
	public GameObject moneyTextEffect;
	public ParticleSystem newItemEffect;

	[Header("UI")]
	public TextMeshProUGUI haveMoneyText;
	public TextMeshProUGUI addMoneyText;
	public TextMeshProUGUI upgradeMoneyText;
	public TextMeshProUGUI speedMoneyText;
	public TextMeshProUGUI incomeMoneyText;
	public GameObject addUIButton;
	public GameObject upgradeUIButton;
	public GameObject speedUIButton;
	public GameObject incomeUIButton;
	public TextMeshProUGUI moneyForSecText;

	[Header("Settings")]
	public Settings settings;
	public bool isVibrate = true;
	public bool isSoundEffect = true;
	public AudioSource sound;

	[Header("Start")] 
	public GameObject blastHandIcon;
	public GameObject rotateHandIcon;

	private float flowTime = 0;
	private bool flowed = true;
	private bool isReset = false;
	private int maxFountainLevel = 0;
	private int currentFountainSetLevel = 0;
	[HideInInspector] public int checkFountainCount = 0; //For error on splash

	[HideInInspector] public static Player player { get; private set; }

	private float TextedMoney(float money)
	{
		return (float)Math.Round((decimal)money, 2);
	}

	private void Awake()
	{
		if (player == null) player = this;
	}

	private void Start()
	{
		fountainLevel = 1;
	    speedLevel = 1;
	    incomeLevel = 1;

		//Max fountain level
		maxFountainLevel = 0;

		for (int i = 0; i < fountainSets.Count; i++)
		{
			for (int j = 0; j < fountainSets[i].fountains.Count; j++)
			{
				maxFountainLevel++;
			}
		}

		Load();

		CheckLevels();

		if (money <= 0)
		{
			blastHandIcon.SetActive(true);
			rotateHandIcon.SetActive(false);
		}
		else
		{
			blastHandIcon.SetActive(false);
			rotateHandIcon.SetActive(false);
		}

		if (checkFountainCount != openFountainCount) checkFountainCount = openFountainCount;

		lastFountainSet = currentFountainSet;
	}

	private void Update()
	{
		if (flowed)
		{
			StopAllCoroutines();
			isReset = false;
			StartCoroutine(FlowWater());
		}

		if (Input.touchCount != 0)
		{
			currentSpeed = currentSpeedNormal * fastSpeedFactorOnTouch;

			if (blastHandIcon.activeInHierarchy || rotateHandIcon.activeInHierarchy)
			{
				Touch touch = Input.GetTouch(0);
				if (blastHandIcon.activeInHierarchy && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
				{
					blastHandIcon.SetActive(false);
					rotateHandIcon.SetActive(true);
				}
			}
		}
		else currentSpeed = currentSpeedNormal;

		haveMoneyText.text = TextedMoney(money).ToString();

		CheckButtons();

		CheckSound();

		//Flow Money For Seconds
		float moneyForSec = moneyIncomeForEachFlow * currentMoneyIncrease * (currentFountainSetLevel + 1) * openFountainCount;
		moneyForSec /= (flowWaitTime / currentSpeed) + (currentFountainSet.fountains[openFountainCount - 1].pipeOnShaderTransitionSeconds / currentSpeed);
		moneyForSecText.text = $"{TextedMoney(moneyForSec)}/sec";
	}

	private void FixedUpdate()
	{
		if (Input.touchCount != 0)
		{
			var touch = Input.GetTouch(0);

			if (touch.phase == TouchPhase.Moved)
			{
				rb.AddTorque(0, touch.deltaPosition.x * -rotateSpeed, 0);
				rotatingObj.transform.rotation = rb.transform.rotation;
			}

			if (rotateHandIcon.activeInHierarchy && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
			{
				rotateHandIcon.SetActive(false);
			}
		}
	}

	private IEnumerator FlowWater()
	{
		flowed = false;

		for (int i = checkFountainCount - 1; i > -1; i--)
		{
			currentFountainSet.fountains[i].contortAlong.SplashWater();

			//Money
			float moneyAmount = moneyIncomeForEachFlow * currentMoneyIncrease * (currentFountainSetLevel + 1);
			GameObject moneyObj = Instantiate(moneyTextEffect, currentFountainSet.fountains[i].moneyTextEffectPos.transform.position, Quaternion.Euler(0, -90, 0));
			moneyObj.GetComponent<TextMeshPro>().text = "$" + TextedMoney(moneyAmount).ToString();
			moneyObj.transform.rotation = camera.transform.rotation;
			money += moneyAmount;

			if (isReset) yield break;

			yield return new WaitUntil(() => currentFountainSet.fountains[i].contortAlong.isSplashed || isReset);

			if (isReset) yield break;
		}

		//Pipe Shader
		MeshRenderer meshPipe = currentFountainSet.fountains[checkFountainCount - 1].pipeInnerObj.GetComponent<MeshRenderer>();
		meshPipe.material.SetFloat("_Panner", currentFountainSet.fountains[checkFountainCount - 1].pipeOnShaderStartValue);
		meshPipe.material.DOFloat(currentFountainSet.fountains[checkFountainCount - 1].pipeOnShaderEndValue, "_Panner", currentFountainSet.fountains[checkFountainCount - 1].pipeOnShaderTransitionSeconds / currentSpeed);

		if (isReset) yield break;

		//Wait
		yield return new WaitForSeconds(currentFountainSet.fountains[checkFountainCount - 1].pipeOnShaderTransitionSeconds / currentSpeed);

		//Flow Finish
		flowed = true;

		if (checkFountainCount != openFountainCount) checkFountainCount = openFountainCount;
	}

	public void CheckCount()
	{
		if (checkFountainCount != openFountainCount) checkFountainCount = openFountainCount;
	}

	/* Old Flow Loop
	 
		 for (int i = openFountainCount - 1; i > -1; i--)
			{
				float moneyAmount = moneyIncomeForEachFlow * currentMoneyIncrease * (currentFountainSetLevel + 1);
	
				//Shader On
				MeshRenderer meshFlow = currentFountainSet.fountains[i].flowObj.GetComponent<MeshRenderer>();
				meshFlow.material = waterfallOnMat;
				meshFlow.material.SetFloat("_ProgressBorder", shaderOnStartValue);
				meshFlow.material.DOFloat(shaderOnEndValue, "_ProgressBorder", shaderOnTransitionSeconds / currentSpeed);
				meshFlow.material.SetFloat("_Transparency", 1.5f);
				meshFlow.material.DOFloat(1, "_Transparency", shaderOnTransitionSeconds / currentSpeed);
	
		currentFountainSet.fountains[i].contortAlong.SplashWater();
	
				//Money
				GameObject moneyObj = Instantiate(moneyTextEffect, currentFountainSet.fountains[i].moneyTextEffectPos.transform.position, Quaternion.Euler(0, -90, 0));
		moneyObj.GetComponent<TextMeshPro>().text = "$" + TextedMoney(moneyAmount).ToString();
		moneyObj.transform.rotation = camera.transform.rotation;
				money += moneyAmount;
	
				yield return new WaitUntil(() => !currentFountainSet.fountains[i].contortAlong.isSplashed);
	
		//Wait
		//yield return new WaitForSeconds((shaderOnTransitionSeconds / currentSpeed) - 0.1f);
	
		//currentFountainSet.fountains[i].flowGroundObj.transform.GetChild(0).gameObject.SetActive(true);
	
		//Wait
		//yield return new WaitForSeconds(flowWaitTime / currentSpeed - ((shaderOnTransitionSeconds / currentSpeed) - 0.1f) - shaderOffTransitionSeconds / currentSpeed);
	
		//Shader Off
		meshFlow.material = waterfallOffMat;
		meshFlow.material.SetFloat("_ProgressBorder", shaderOffStartValue);
		meshFlow.material.DOFloat(shaderOffEndValue, "_ProgressBorder", shaderOffTransitionSeconds / currentSpeed);
		meshFlow.material.SetFloat("_Transparency", 1);
		meshFlow.material.DOFloat(2, "_Transparency", shaderOnTransitionSeconds / currentSpeed);
	
		//currentFountainSet.fountains[i].flowGroundObj.GetComponent<MeshRenderer>().material.DOFloat(0, "_Transparency", shaderOffTransitionSeconds / currentSpeed);
	
		//Wait
		//yield return new WaitForSeconds(shaderOffTransitionSeconds / currentSpeed);
	
		currentFountainSet.fountains[i].flowGroundObj.transform.GetChild(0).gameObject.SetActive(false);
		currentFountainSet.fountains[i].flowGroundObj.GetComponent<MeshRenderer>().material.DOFloat(1, "_Transparency", shaderOnTransitionSeconds / currentSpeed);
	}

	*/

	private void CheckLevels()
	{
		//Level money counts and texts
		currentFountainLevelMoney = 0;

		for (int i = 0; i < fountainLevel; i++)
		{
			currentFountainLevelMoney += (i + 1) * fountainMoneyByLevel;
		}

		currentSpeedLevelMoney = speedLevel * speedMoneyByLevel;
		currentIncomeLevelMoney = incomeLevel * incomeMoneyByLevel;

		//UI
		if (fountainLevel >= maxFountainLevel)
		{
			addMoneyText.text = "Max";
		}
		else
		{
			addMoneyText.text = "$" + TextedMoney(currentFountainLevelMoney).ToString();
		}

		upgradeMoneyText.text = "$" + TextedMoney(currentFountainLevelMoney).ToString();

		if (speedLevel >= maxSpeedLevel)
		{
			speedMoneyText.text = "Max";
		}
		else
		{
			speedMoneyText.text = "$" + TextedMoney(currentSpeedLevelMoney).ToString();
		}

		if (incomeLevel >= maxIncomeLevel)
		{
			incomeMoneyText.text = "Max";
		}
		else
		{
			incomeMoneyText.text = "$" + TextedMoney(currentIncomeLevelMoney).ToString();
		}

		//Current Fountain Level
		int checkLevel = 1;
		bool isChecked = false;

		for (int i = 0; i < fountainSets.Count; i++)
		{
			for (int j = 0; j < fountainSets[i].fountains.Count; j++)
			{
				if (fountainLevel <= checkLevel)
				{
					currentFountainSet = fountainSets[i];
					openFountainCount = j + 1;
					currentFountainSetLevel = i;
					isChecked = true;
					break;
				}
				else
				{
					checkLevel++;
					continue;
				}
			}
			if (isChecked) break;
		}

		for (int i = 0; i < fountainSets.Count; i++)
		{
			if (!fountainSets[i].Equals(currentFountainSet))
			{
				for (int j = 0; j < fountainSets[i].fountains.Count; j++)
				{
					if(j == 0) fountainSets[i].fountains[j].objects[0].SetActive(true);
					else fountainSets[i].fountains[j].objects[0].SetActive(false);
				}
				fountainSets[i].otherObjs[0].transform.position = closePos.position;
			}
			else
			{
				for (int j = 0; j < fountainSets[i].fountains.Count; j++)
				{
					if (j < openFountainCount)
					{
						fountainSets[i].fountains[j].objects[0].SetActive(true);
						fountainSets[i].fountains[j].contortAlong.Init();
					}
					
					else fountainSets[i].fountains[j].objects[0].SetActive(false);
				}
				fountainSets[i].otherObjs[0].transform.position = openPos.position;
			}
		}

		//Pipe
		for (int i = 0; i < fountainSets.Count; i++)
		{
			for (int j = 0; j < fountainSets[i].fountains.Count; j++)
			{
				fountainSets[i].fountains[j].pipe.SetActive(false);
			}
		}

		currentFountainSet.fountains[openFountainCount - 1].pipe.SetActive(true);

		//Speed
		currentSpeedNormal = speedStartCount + speedIncreaseByLevel * (speedLevel - 1);

		//Income
		currentMoneyIncrease = incomeStartCount + incomeIncreaseByLevel * (incomeLevel - 1);

		Save();
	}

	public void BuyFountainUpgrade()
	{
		if (money >= currentFountainLevelMoney)
		{
			fountainLevel++;
			money -= currentFountainLevelMoney;
			GenerateVibration();
			newItemEffect.Play();
			CheckLevels();
			ResetFountains();
			isReset = true;
			flowed = true;
		}
		Save();
	}

	public void BuySpeedUpgrade()
	{
		if (money >= currentSpeedLevelMoney)
		{
			speedLevel++;
			money -= currentSpeedLevelMoney;
			GenerateVibration();
			CheckLevels();
			ResetFountains();
			flowed = true;
		}
		Save();
	}

	public void BuyIncomeUpgrade()
	{
		if (money >= currentIncomeLevelMoney)
		{
			incomeLevel++;
			money -= currentIncomeLevelMoney;
			GenerateVibration();
			CheckLevels();
			ResetFountains();
			flowed = true;
		}
		Save();
	}

	private void ResetFountains()
	{
		/*if (!lastFountainSet.Equals(currentFountainSet))
		{
			isReset = true;

			for (int i = openFountainCount - 1; i > -1; i--)
			{
				currentFountainSet.fountains[i].contortAlong.StopAllCoroutines();
				currentFountainSet.fountains[i].contortAlong.RestartObject();
			}

			flowed = true;

			lastFountainSet = currentFountainSet;

			if (checkFountainCount != openFountainCount) checkFountainCount = openFountainCount;

			Debug.Log("Different fountain set");
		}
		else
		{
			for (int i = openFountainCount - 1; i > -1; i--)
			{
				currentFountainSet.fountains[i].contortAlong.CloseMesh();
			}
		}*/

		if (checkFountainCount != openFountainCount) checkFountainCount = openFountainCount;

		for (int i = openFountainCount - 1; i > -1; i--)
		{
			currentFountainSet.fountains[i].contortAlong.CloseMesh();
		}
	}

	private void CheckButtons()
	{
		//Add button
		if (fountainLevel >= maxFountainLevel || currentFountainLevelMoney > money || openFountainCount == currentFountainSet.fountains.Count)
		{
			addUIButton.GetComponent<Button>().interactable = false;
		}
		else
		{
			addUIButton.GetComponent<Button>().interactable = true;
		}

		//Upgrade button
		if (openFountainCount != currentFountainSet.fountains.Count || fountainLevel >= maxFountainLevel)
		{
			upgradeUIButton.SetActive(false);
		}
		else
		{
			if (currentFountainLevelMoney > money)
			{
				upgradeUIButton.SetActive(true);
				upgradeUIButton.GetComponent<Button>().interactable = false;
			}
			else
			{
				upgradeUIButton.SetActive(true);
				upgradeUIButton.GetComponent<Button>().interactable = true;
			}
		}

		//Speed button
		if (speedLevel >= maxSpeedLevel || currentSpeedLevelMoney > money)
		{
			speedUIButton.GetComponent<Button>().interactable = false;
		}
		else
		{
			speedUIButton.GetComponent<Button>().interactable = true;
		}

		//Income button
		if (incomeLevel >= maxIncomeLevel || currentIncomeLevelMoney > money)
		{
			incomeUIButton.GetComponent<Button>().interactable = false;
		}
		else
		{
			incomeUIButton.GetComponent<Button>().interactable = true;
		}
	}

	private void Save()
	{
		PlayerPrefs.SetFloat("Money", money);
		PlayerPrefs.SetInt("FountainLevel", fountainLevel);
		PlayerPrefs.SetInt("SpeedLevel", speedLevel);
		PlayerPrefs.SetInt("IncomeLevel", incomeLevel);

		if (isSoundEffect) PlayerPrefs.SetInt("Sound", 1);
		else PlayerPrefs.SetInt("Sound", 0);

		if (isVibrate) PlayerPrefs.SetInt("Vibrate", 1);
		else PlayerPrefs.SetInt("Vibrate", 0);
	}

	private void Load()
	{
		if(PlayerPrefs.HasKey("Money")) money = PlayerPrefs.GetFloat("Money");
		if(PlayerPrefs.HasKey("FountainLevel")) fountainLevel = PlayerPrefs.GetInt("FountainLevel");
		if(PlayerPrefs.HasKey("SpeedLevel")) speedLevel = PlayerPrefs.GetInt("SpeedLevel");
		if (PlayerPrefs.HasKey("IncomeLevel")) incomeLevel = PlayerPrefs.GetInt("IncomeLevel");

		if (PlayerPrefs.HasKey("Vibrate"))
		{
			if (PlayerPrefs.GetInt("Vibrate") == 1)
			{
				isVibrate = true;
				settings.vibrate.GetComponent<Image>().sprite = settings.vibrateOn;
				settings.isOpenVibrate = true;
			}
			else
			{
				isVibrate = false;
				settings.vibrate.GetComponent<Image>().sprite = settings.vibrateOff;
				settings.isOpenVibrate = false;
			}
		}
		else
		{
			isVibrate = true;
			settings.vibrate.GetComponent<Image>().sprite = settings.vibrateOn;
			settings.isOpenVibrate = true;
		}

		if (PlayerPrefs.HasKey("Sound"))
		{
			if (PlayerPrefs.GetInt("Sound") == 1)
			{
				isSoundEffect = true;
				settings.sound.GetComponent<Image>().sprite = settings.soundOn;
				settings.isOpenSound = true;
			}
			else
			{
				isSoundEffect = false;
				settings.sound.GetComponent<Image>().sprite = settings.soundOff;
				settings.isOpenSound = false;
			}
		}
		else
		{
			isSoundEffect = true;
			settings.sound.GetComponent<Image>().sprite = settings.soundOn;
			settings.isOpenSound = true;
		}
	}

	public void ResetSave()
	{
		money = 0;
		fountainLevel = 1;
		speedLevel = 1;
		incomeLevel = 1;

		CheckLevels();
		//ResetFountains();
	}

	private void GenerateVibration()
	{
		if (!isVibrate) return;

		MMVibrationManager.StopAllHaptics();

		MMVibrationManager.TransientHaptic(0.85f, 0.05f, true, this);
	}

	private void CheckSound()
	{
		if(isSoundEffect) sound.mute = false;
		else sound.mute = true;
	}

	private void OnApplicationQuit()
	{
		Save();
	}
}
