using System;
using System.IO;
using System.Text.RegularExpressions;

namespace TurtleMineShared.Utils
{
    /// <summary>
    /// 配置文件读取辅助类
    /// </summary>
    public static class ConfigHelper
    {
        /// <summary>
        /// 获取提交者姓名
        /// 从用户AppData\Roaming\小九\config.json中读取staffName字段
        /// </summary>
        /// <returns>提交者姓名，如果获取失败则返回空字符串</returns>
        public static string GetStaffName()
        {
            try
            {
                var roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var configPath = Path.Combine(roamingPath, "小九", "config.json");
                
                if (!File.Exists(configPath))
                {
                    return string.Empty;
                }
                
                var configContent = File.ReadAllText(configPath);
                
                // 使用正则表达式提取 staffName 字段的值
                var regex = new Regex(@"""staffName""\s*:\s*""([^""]*)", RegexOptions.IgnoreCase);
                var match = regex.Match(configContent);
                
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                
                return string.Empty;
            }
            catch
            {
                // 如果读取失败，返回空字符串
                return string.Empty;
            }
        }
    }
} 