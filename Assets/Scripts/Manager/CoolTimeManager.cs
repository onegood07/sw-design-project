using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoolTimeManager : MonoBehaviour
{
    public static CoolTimeManager Instance;
    private Dictionary<int, float> coolTimes;
    private List<int> currentCooling;

    private float Cooltemp;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
    private void Start()
    {
        coolTimes = new Dictionary<int, float>();
        currentCooling = new List<int>();
    }

    private void Update()
    {
        //현재 리스트에 들어있는 모든 요소들을 돌면서 쿨타임을 확인
        for (int i = currentCooling.Count - 1; i >= 0; --i)
        {
            //매 프레임마다 쿨타임 갱신
            Cooltemp = coolTimes[currentCooling[i]] = coolTimes[currentCooling[i]] - Time.deltaTime;

            //쿨타임이 끝났다면 리스트에서 요소 제거
            if(Cooltemp < 0) { currentCooling.RemoveAt(i); }
        }
    }
    public void AddCooltimeQueue(int itemID, float originCooltime)
    {
        // coolTimes.TryAdd(itemID, originCooltime);

        coolTimes[itemID] = originCooltime;
        currentCooling.Add(itemID);
    }

    public float GetCurrentCooltime(int itemID,float originCooltime)
    {
        if (!coolTimes.ContainsKey(itemID))
        {
            AddCooltimeQueue(itemID,originCooltime);
            return 0;
        }
        float cooltime;
        bool isSuccess = coolTimes.TryGetValue(itemID, out cooltime);

        if (isSuccess) { return cooltime; }
        else { return 0; }
    }
}