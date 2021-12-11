//実態を持たないもの
public class Element : ChemistryObject
{
    protected override void Init()
    {
        IsMaterial = false;
    }
}
