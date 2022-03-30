public interface IStatus
{
    //Hp or 耐久値を減らす
    public void ReduceHp();
    //死んでいるかどうか
    public bool IsDie { get; set; }
}
