namespace BDP.Core.Information
{
    /// <summary>所有芯片信息消费者共用的只读读取契约。</summary>
    public interface IChipInformationReader
    {
        /// <summary>根据统一中性输入读取公开信息模型。</summary>
        ChipInformationModel Read(ChipInformationParseInput input);
    }
}
