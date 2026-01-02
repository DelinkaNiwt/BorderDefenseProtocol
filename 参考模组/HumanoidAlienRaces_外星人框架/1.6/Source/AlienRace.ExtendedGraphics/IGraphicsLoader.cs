namespace AlienRace.ExtendedGraphics;

public interface IGraphicsLoader
{
	void LoadAllGraphics(string source, params AlienPartGenerator.ExtendedGraphicTop[] graphicTops);
}
