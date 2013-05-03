using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;

namespace WeTongji.Api.Domain
{
    public enum FavoriteIndex : uint
    {
        kActivity,
        kPeopleOfWeek,
        kTongjiNews,
        kAroundNews,
        kOfficialNotes,
        kClubNews,

        FavoriteTypeCount
    }

    [Table()]
    public class FavoriteObject
    {
        /// <summary>
        /// Refers to FavoriteIndex
        /// </summary>
        [Column(IsPrimaryKey = true)]
        public uint Id { get; set; }

        /// <summary>
        /// {Id0}_{Id1}_.....{Idn}
        /// </summary>
        [Column()]
        public String Value { get; set; }

        public Boolean Contains(int idx)
        {
            if (!String.IsNullOrEmpty(Value))
            {
                var src = TryParseValue();
                return src.Contains(idx);
            }
            else
                return false;
        }

        public void Add(int idx)
        {
            if (!Contains(idx))
            {
                Value = String.IsNullOrEmpty(Value) ? idx.ToString() : Value + "_" + idx;
            }
        }

        public void Remove(int idx)
        {
            var src = TryParseValue();

            if (src != null && src.Count() > 0)
            {
                var result = src.Except(new int[] { idx });
                var sb = new StringBuilder();

                if (result.Count() > 0)
                {
                    foreach (var item in result)
                    {
                        sb.AppendFormat("{0}_", item);
                    }

                    Value = sb.ToString(0, sb.Length - 1);
                }
                else
                {
                    Value = String.Empty;
                }
            }
        }

        public IEnumerable<int> TryParseValue()
        {
            var srcs = Value.Split('_');
            int count = srcs.Count();

            int[] result = new int[count];

            for (int i = 0; i < count; ++i)
            {
                if (!int.TryParse(srcs[i], out result[i]))
                {
                    return null;
                }
            }

            return result;
        }
    }
}
