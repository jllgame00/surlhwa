// RecipeStepType.cs
// 레시피를 구성하는 각 단계의 타입 정의

public enum RecipeStepType
{
    Pour,        // 일반 붓기 (Build)
    FloatPour,   // 천천히 붓기 (Float)
    StirGauge,   // 젓기 게이지
    ShakeGauge   // 쉐이킹 게이지
}
