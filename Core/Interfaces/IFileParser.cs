namespace Core.Interfaces;

public interface IFileParser<T>
{
    T Parse(string filePath);
}
