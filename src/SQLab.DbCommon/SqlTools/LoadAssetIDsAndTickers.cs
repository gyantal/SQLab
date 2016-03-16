using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbCommon
{
    public static partial class SqlTools
    {
        public static async Task<Dictionary<string, Tuple<IAssetID, string>>> LoadAssetIdsForTickers(List<string> p_tickers)
        {
            // 1. 
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.Append("SELECT * FROM [dbo].[AllTickersView] WHERE Ticker in (");
            for (int i = 0; i < p_tickers.Count; i++)
            {
                if (i > 0)
                    sqlBuilder.Append($" , ");
                sqlBuilder.Append($"'{p_tickers[i]}'");
            }
            sqlBuilder.Append(@")");

            var sqlResult = await SqlTools.ExecuteSqlQueryAsync(sqlBuilder.ToString(), null, null);
            var result = sqlResult[0].ToDictionary(r => (string)(r[2]), r => new Tuple<IAssetID, string>(DbUtils.MakeAssetID((AssetType)(int)r[0], (int)r[1]), (string)r[3]));
            return result;
        }

    }
}
