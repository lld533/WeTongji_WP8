using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeTongji.Api.Domain
{
    public class CalendarGroup<T> : List<T> where T : CalendarNode
    {
        public CalendarGroup(DateTime key, IEnumerable<T> items)
        {
            this.Key = key;
            this.AddRange(items.OrderBy((T) => T));
        }

        public DateTime Key
        {
            get;
            set;
        }

        public Boolean IsToday
        {
            get 
            { 
                return Key == DateTime.Now.Date; 
            }
        }
    }

    public static class CalendarGroupUtil
    {
        public static CalendarNode GetNextCalendarNode(this List<CalendarGroup<CalendarNode>> list)
        {
            //...Remove NoArrangement Node
            for (int i = 0; i < list.Count; ++i)
            {
                var group = list[i];

                if (group.Count > 0 && group.Last().IsNoArrangementNode)
                {
                    group.RemoveAt(group.Count - 1);
                    break;
                }
            }

            CalendarNode result = null;
            var yesterday = (DateTime.Now - TimeSpan.FromDays(1)).Date;
            var today = DateTime.Now.Date;

            var yesterdayGroup = list.Where((g) => g.Key == yesterday).SingleOrDefault();
            var todayGroup = list.Where((g) => g.Key == today).SingleOrDefault();

            if (yesterdayGroup != null)
            {
                if (yesterdayGroup.Count == 1 && yesterdayGroup.Single().IsNoArrangementNode)
                {
                    list.Remove(yesterdayGroup);
                }
            }

            if (todayGroup == null)
            {
                int groupIdx = -1;

                groupIdx = list.Where((g) => g.Key < today).Count();
                list.Insert(groupIdx, new CalendarGroup<CalendarNode>(today, new CalendarNode[] { CalendarNode.NoArrangementNode }));
                result = list[groupIdx].Single();
            }
            else
            {
                if (todayGroup.Count() == 0)
                {
                    todayGroup.Add(CalendarNode.NoArrangementNode);
                    result = todayGroup.First();
                }
                else
                {
                    int nodeIdx = todayGroup.Where((node) => node.BeginTime < DateTime.Now).Count();

                    if (nodeIdx < todayGroup.Count())
                    {
                        result = todayGroup[nodeIdx];
                    }
                    else
                    {
                        result = CalendarNode.NoArrangementNode;
                    }
                }
            }

            return result;
        }

        public static void InsertCalendarNode(this List<CalendarGroup<CalendarNode>> list, CalendarNode node)
        {
            //...Refresh
            list.GetNextCalendarNode();

            var g = list.Where((group) => group.Key == node.BeginTime.Date).SingleOrDefault();

            //...The date of the activity exists in Agenda
            if (g != null)
            {
                //...Remove potential no arrangement node
                if (g.Key == DateTime.Now.Date && g.Count == 1 && g.Single().IsNoArrangementNode)
                {
                    g.Clear();
                }

                var targetNode = g.Where((n) => n == node).SingleOrDefault();
                if (targetNode == null)
                {
                    int idx = g.Where((n) => n.BeginTime < node.BeginTime).Count();
                    g.Insert(idx, node);
                }
            }
            //...The date of the activity does not exist in Agenda
            else
            {
                int idx = list.Where((group) => group.Key < node.BeginTime.Date).Count();
                list.Insert(idx, new CalendarGroup<CalendarNode>(node.BeginTime.Date, new CalendarNode[] { node }));
            }

            list.GetNextCalendarNode();
        }

        public static void RemoveCalendarNode(this List<CalendarGroup<CalendarNode>> list, CalendarNode node)
        {
            var group = list.Where((g) => g.Key == node.BeginTime.Date).SingleOrDefault();

            if (group != null)
            {
                var target = group.Where((n) => n.CompareTo(node) == 0).SingleOrDefault();

                if (target != null)
                    group.Remove(target);

                if (group.Count() == 0)
                {
                    if (group.Key != DateTime.Now.Date)
                        list.Remove(group);
                    else
                        group.Add(CalendarNode.NoArrangementNode);
                }
            }
        }
    }
}
