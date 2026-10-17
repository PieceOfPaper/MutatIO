namespace MutatIO.Input
{
    /// <summary>
    /// 첫 화면에서 선택하는 네 가지 이동 조작 방식입니다.
    /// 숫자 값은 PlayerPrefs 저장에 쓰는 안정적인 식별자이므로 바꾸지 않습니다. 표시 문구와 분리합니다.
    /// </summary>
    public enum ControlMode
    {
        Wasd = 1,
        ArrowKeys = 2,
        VirtualPad = 3,
        PointerFollow = 4,
    }
}
