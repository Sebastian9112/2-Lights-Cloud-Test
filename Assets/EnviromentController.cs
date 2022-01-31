using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[Serializable]
public struct DayTime
{
    [Range(0, 1)] public float value;
    [Range(0, 255)] public byte scale;// 0 stops day cycle
}

[ExecuteAlways]
public class EnviromentController : MonoBehaviour
{
    public DayTime dayTime;

    [Range(0, 3600)] public float simRate = 1;
    [Range(-90, 90)] public float latitude = 45;
    [Range(-180, 180)] public float longitude = 0;

    public VolumeProfile volumeProfile;

    [Header("Sun + Moon")]
    public Transform[] lightTransform = new Transform[2];
    [Range(-90, 90)] public float[] latitudeOffset = new float[2] { 23.5f, -18.3f };
    [Range(-180, 180)] public float[] longitudeOffset = new float[2] { 180, 0 };

    //static int cookies = 24;
    //public Texture2D[] moonCookies = new Texture2D[cookies];

    [Range(-0.75f, 0.75f)] public float shadowSwitch = -0.0625f;
    [Range(0, 0.25f)] public float shadowBlend = 0.0625f;

    //static float interval = 360f / cookies;
    //static float halfInterval = interval / 2;

    PhysicallyBasedSky pbs;
    
    HDAdditionalLightData[] lightData = new HDAdditionalLightData[2];// sun, moon

    float updateTime;

    void OnValidate()
    {
        Setup();
        UpdateSky();
    }

    void Start()
    {
        Setup();
    }

    void LateUpdate()
    {
        if (dayTime.scale != 0)
        {
            dayTime.value += dayTime.scale / 86400f * Time.deltaTime;
            if (dayTime.value >= 1)
                dayTime.value -= 1;

            if (Time.time >= updateTime)
            {
                updateTime += simRate;
                UpdateSky();
            }
        }
    }

    void Setup()
    {
        updateTime = Time.time;

        PhysicallyBasedSky pbsTmp;
        if (volumeProfile.TryGet(out pbsTmp))
        {
            pbs = pbsTmp;
            pbs.planetRotation.value = (Quaternion.Euler(-90 + latitude, 0, 0) * Quaternion.Euler(0, longitude, 0)).eulerAngles;
        }

        for (int i = 0; i < 2; i++)
        {
            lightData[i] = lightTransform[i].GetComponent<HDAdditionalLightData>();
        }

        //var cookie = (longitudeOffset[1] + halfInterval) / interval;
        //if (cookie < 0)
        //    cookie += cookies;

        //lightData[1].surfaceTexture = moonCookies[(int)cookie];
    }

    void UpdateSky()
    {
        var progress = dayTime.value * 360;
        var fixedRot = Quaternion.Euler(90 - latitude, 180, 0);

        float dotSun = Vector3.Dot(lightTransform[0].rotation * Vector3.forward, Vector3.down);
        for (int i = 0; i < 2; i++)
        {
            lightTransform[i].localRotation = fixedRot * Quaternion.Euler(latitudeOffset[i], progress + longitudeOffset[i], 0);
            lightData[i].shadowDimmer = 1 - Mathf.Clamp01((i == 0 ? shadowSwitch + shadowBlend - dotSun : dotSun - shadowSwitch + shadowBlend) / shadowBlend);
            lightData[i].EnableShadows(dotSun >= shadowSwitch ? (i == 0 ? true : false) : (i == 0 ? false : true));
        }

        pbs.spaceRotation.value = (fixedRot * Quaternion.Euler(0, progress, 0)).eulerAngles;
    }
}
