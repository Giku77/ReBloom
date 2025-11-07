public interface IProduct
{
    // 모든 제품이 공통으로 가지는 속성
    public string productName { get; set; }

    // 제품별로 다르게 구현할 초기화 메서드
    public void Init();
}