using UnityEngine;

[System.Serializable]
public class RecipeStep
{
    public RecipeStepType type;
    public string ingredientName;

    // Pour 계열
    [Range(0f, 1f)] public float targetAmount01 = 0.5f;  // 목표량(정규화)
    [Range(0f, 1f)] public float tolerance01 = 0.08f;    // 허용 오차

    // Float 계열(속도 제한)
    public float maxPourSpeed = 0.35f;  // 초당 증가량(정규화) 제한

    // Gauge 계열
    [Range(0.5f, 1f)] public float targetMin01 = 0.5f;   // 50~100%
    [Range(0.5f, 1f)] public float targetMax01 = 0.9f;   // 50~100%
    public float gaugeSpeed = 0.9f;                      // 게이지 이동 속도
}
