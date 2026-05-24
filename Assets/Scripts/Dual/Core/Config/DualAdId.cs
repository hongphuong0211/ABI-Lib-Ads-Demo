public class DualAdId
{
  public readonly string admobId;
  public readonly string maxId;

  public DualAdId(
    string admobId,
    string maxId
  )
  {
    this.admobId = admobId;
    this.maxId = maxId;
  }

  public class Builder
  {
    private string admobId;
    private string maxId;

    public Builder SetAdMobId(string admobId)
    {
      this.admobId = admobId;
      return this;
    }

    public Builder SetMAXId(string maxId)
    {
      this.maxId = maxId;
      return this;
    }

    public DualAdId Build()
    {
      return new DualAdId(
        admobId: admobId,
        maxId: maxId
      );
    }
  }
}
