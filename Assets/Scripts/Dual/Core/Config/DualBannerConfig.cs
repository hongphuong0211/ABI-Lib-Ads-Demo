public class DualBannerConfig
{
  public readonly Position position;
  public readonly int xPos;
  public readonly int yPos;
  public readonly bool loadAfterInit;

  public DualBannerConfig(
    Position position,
    int xPos,
    int yPos,
    bool loadAfterInit
  )
  {
    this.position = position;
    this.xPos = xPos;
    this.yPos = yPos;
    this.loadAfterInit = loadAfterInit;
  }


  public class Builder
  {
    private Position position = Position.BottomCenter;
    private int xPos;
    private int yPos;
    private bool loadAfterInit = true;

    public Builder SetPosition(Position position)
    {
      this.position = position;
      return this;
    }

    public Builder XPos(int xPos)
    {
      this.xPos = xPos;
      return this;
    }

    public Builder YPos(int yPos)
    {
      this.yPos = yPos;
      return this;
    }

    public Builder SetLoadAfterInit(bool loadAfterInit)
    {
      this.loadAfterInit = loadAfterInit;
      return this;
    }

    public DualBannerConfig Build()
    {
      return new DualBannerConfig(
        position: position,
        xPos: xPos,
        yPos: yPos,
        loadAfterInit: loadAfterInit
      );
    }
  }

  public enum Position
  {
    TopLeft,
    TopCenter,
    TopRight,
    Centered,
    CenterLeft,
    CenterRight,
    BottomLeft,
    BottomCenter,
    BottomRight
  }
}
