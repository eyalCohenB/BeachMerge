public enum CellState { Locked, Filled, Empty }

public class BoardCell
{
    public int row;
    public int col;
    public CellState state;
    public ItemData item;
    public MergeItem occupant;
}
