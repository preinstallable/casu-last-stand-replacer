using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Video;

namespace CULastStandReplacer
{
	[BepInPlugin(MyGUID, PluginName, VersionString)]
    public class NoBellPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "preinstallable.nobell";
        private const string PluginName = "I Didn't Hear No Bell";
        private const string VersionString = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{PluginName} v{VersionString} is loading...");

            Harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.LogInfo($"{PluginName} loaded successfully!");
        }
    }
	
	[HarmonyPatch(typeof(Body))]
    internal class BodyPatches
    {
        [HarmonyPatch(typeof(Body), "TryLastStand")]
        [HarmonyPrefix]
        static bool TryLastStand(Body __instance) // nts: use ref type __fieldName if it's one of those weird static (or was it instance) fields that you need an og reference to
        {
            float num = __instance.lastLastChanceHappiness.Evaluate(__instance.lastHappiness[9]);
			__instance.triedRollingLastStand = true;
			if (UnityEngine.Random.value < num)
			{
				__instance.brainHealth = UnityEngine.Random.Range(75f, 90f);
				__instance.hunger = Mathf.Lerp(__instance.hunger, 100f, 0.5f);
				__instance.thirst = Mathf.Lerp(__instance.thirst, 100f, 0.5f);
				__instance.weightOffset = Mathf.Lerp(__instance.weightOffset, 0f, 0.15f);
				__instance.sicknessAmount = Mathf.Lerp(__instance.sicknessAmount, 0f, 0.3f);
				__instance.bloodVolume = Mathf.Max(__instance.bloodVolume, 50f);
				__instance.heartRate = 120f;
				__instance.fibrillationProgress = 0f;
				__instance.bloodPressure = 135f;
				__instance.bloodVesselSize = 1f;
				__instance.bloodOxygen = 100f;
				__instance.bloodViscosity = 0f;
				__instance.strokeAmount = 0f;
				__instance.hasPulmonaryEmbolism = false;
				__instance.heartRatePressureOffset = 0f;
				__instance.respiratoryRate = 100f;
				__instance.septicShock *= 0.4f;
				__instance.lastStandTime = 300f;
				__instance.happiness = 10f;
				__instance.venomCurrent = 0f;
				__instance.venomTotal = 0f;
				__instance.energy = 100f;
				__instance.antibioticImmunityTime = 120f;
				__instance.caffeinated = 200f;
				__instance.hemothorax *= 0.5f;
				__instance.temperature = 37f;
				__instance.radiationSickness *= 0.2f;
				__instance.internalBleeding *= 0.05f;
				__instance.clawHealth = Mathf.Max(__instance.clawHealth, 80f);
				foreach (Limb limb in __instance.limbs)
				{
					limb.muscleHealth = Mathf.Lerp(limb.muscleHealth, 100f, 0.3f);
					limb.infectionAmount *= 0.05f;
					limb.bleedAmount *= 0.05f;
				}
				Painkillers painkillers;
				if (__instance.TryGetComponent<Painkillers>(out painkillers))
				{
					painkillers.opiateAmount = 0f;
					painkillers.opiateTolerance = 0f;
					painkillers.opiateReception = 0f;
					painkillers.actualOpiateReception = 0f;
				}
				CoUtils.instance.CancelAll();
				Sound.Play("observerlaugh", Vector2.zero, true, false, null, 1f, 1f, true, true);
				SleepingPills sleepingPills;
				if (__instance.TryGetComponent<SleepingPills>(out sleepingPills))
				{
					UnityEngine.Object.Destroy(sleepingPills);
				}
				Antidepressants antidepressants;
				if (__instance.TryGetComponent<Antidepressants>(out antidepressants))
				{
					UnityEngine.Object.Destroy(antidepressants);
				}
				PlayerCamera.main.StartCoroutine(CustomLastStandSequence(PlayerCamera.main));
				__instance.succesfullyRolledLastStand = true;
				if (Observer.main)
				{
					Observer.main.RolledLastStand();
				}
				if (WorldGeneration.GetRunSettingBool("infinitelaststand"))
				{
					__instance.triedRollingLastStand = false;
				}
			}
            return false;
        }
		
		private static System.Collections.IEnumerator CustomLastStandSequence(PlayerCamera __instance) // This is a much less disgusting hack than using Reflection
		{
			__instance.SetTimeScale(PlayerCamera.SpeedType.Normal, true, false);
			float initialVol = AudioListener.volume;
			AudioListener.volume = 0f;
			float timer = 0f;
			__instance.lastStandPanel.SetActive(true);
			Image oldImage = __instance.lastStandPanel.GetComponent<Image>();
			if (oldImage != null)
			{
				UnityEngine.Object.DestroyImmediate(oldImage);
			}
			
			RawImage rawImg = __instance.lastStandPanel.GetComponent<RawImage>();
			if (rawImg == null)
			{
				rawImg = __instance.lastStandPanel.AddComponent<RawImage>();
			}
			
			rawImg.raycastTarget = false;
			
			RenderTexture renderTexture = new RenderTexture(640, 360, 0)
			{
				filterMode = FilterMode.Point
			};
			
			string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string videoPath = Path.Combine(pluginDir, "video.mp4");
			
			VideoPlayer videoPlayer = __instance.lastStandPanel.GetComponent<VideoPlayer>();
			if (videoPlayer == null)
			{
				videoPlayer = __instance.lastStandPanel.AddComponent<VideoPlayer>();
			}

			videoPlayer.playOnAwake = false;
			videoPlayer.renderMode = VideoRenderMode.RenderTexture;
			videoPlayer.targetTexture = renderTexture;
			videoPlayer.source = VideoSource.Url;
			videoPlayer.url = videoPath;
			
			videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
			
			rawImg.color = Color.black;
			float blackTime = 0f;
			bool playedDrone = false;
			Sound.Play("laststandheartbeat", Vector2.zero, true, false, null, 1f, 1f, true, true);
			videoPlayer.Prepare();
			while (!videoPlayer.isPrepared)
			{
				yield return null;
			}
			while (timer < 6.7f)
			{
				AudioListener.volume = Mathf.MoveTowards(AudioListener.volume, initialVol, initialVol * Time.unscaledDeltaTime * 0.25f);
				__instance.body.bloodOxygen = 100f;
				if (timer > 2f)
				{
					if (!playedDrone)
					{
						playedDrone = true;
						rawImg.texture = renderTexture;
						videoPlayer.Play();
						//Sound.Play("laststanddrone", Vector2.zero, true, false, null, 1f, 1f, true, true); // The video should play sound, right?
					}
					blackTime += Time.unscaledDeltaTime * 0.25f;
					rawImg.color = new Color(blackTime * 1f, blackTime * 1f, blackTime * 1f);
					//int num = (int)(Time.unscaledTime * 3f) % __instance.lastStandImages.Length;
					//img.sprite = __instance.lastStandImages[num];
				}
				timer += Time.unscaledDeltaTime;
				yield return null;
			}
			float alpha = 1f;
			for (;;)
			{
				alpha -= Time.unscaledDeltaTime * 0.25f;
				rawImg.color = new Color(1f, 1f, 1f, alpha);
				if (alpha <= 0f)
				{
					if (renderTexture != null)
					{
						renderTexture.Release();
						UnityEngine.Object.Destroy(renderTexture);
					}
					break;
				}
				yield return null;
			}
			while (videoPlayer.isPlaying)
			{
				yield return null;
			}
			videoPlayer.Stop();
			
			__instance.lastStandPanel.SetActive(false);
			yield break;
		}
    }
}
