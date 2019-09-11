using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VSlice
{

    public class TreeFilter 
    {
        public static class Operators
        {
            public const string EqualTo = "==";
            public const string GreaterThan = ">";
            public const string LessThan = "<";
            public const string NotEqualTo = "!=";
            public const string Contains = "Contains";
            public const string DoesNotContain = "DoesNotContain";
            public const string BeforeDate = "BeforeDate";
            public const string AfterDate = "AfterDate";
            public const string RegularExpression = "Regex";
        }

        public static readonly string[] FilterOperators = new string[] {
            Operators.EqualTo,
            Operators.GreaterThan,
            Operators.LessThan,
            Operators.NotEqualTo,
            Operators.Contains,
            Operators.DoesNotContain,
            Operators.BeforeDate,
            Operators.AfterDate,
            Operators.RegularExpression,
        };

        static int _idSeed = 1;
        public int Id { get; set; } = _idSeed++;
        public string ColumnName { get; set; }
        public string Operator { get; set; } = "==";

        private string _filterText;
        private DateTimeOffset? _filterTextAsDateOffset = null;
        public string FilterText
        {
            get => _filterText;
            set
            {
                _filterText = value;
                IsValidText = IsFilterTextValid();
            }
        }
        public bool IsCaseSensitive { get; set; }
        public bool IsValueColumn { get; set; }
        public bool IsValid=> ColumnName != null && !string.IsNullOrEmpty(FilterText) && IsValidText;

        /// <summary>
        /// True if the text is something we can expect to match to this column
        /// </summary>
        public bool IsValidText { get; private set; }

        private bool IsFilterTextValid()
        {
            if (Operator == Operators.Contains)
            {
                return !string.IsNullOrEmpty(FilterText);
            }

            else if (Operator == Operators.RegularExpression)
            {
                try
                {
                    Regex.IsMatch("", FilterText);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else if (Operator == Operators.BeforeDate || Operator == Operators.AfterDate)
            {
                if (!DateTimeOffset.TryParse(FilterText, out var testDate))
                {
                    return false;
                }

                // Cache the filter text as a date value for better perf
                _filterTextAsDateOffset = testDate;
                return true;
            }

            return true;
        }



        /// -----------------------------------------------------------------------
        /// <summary>
        /// Returns true if the filter matches against this tree item
        /// </summary>
        /// -----------------------------------------------------------------------
        public bool ShouldAllow(IItemData item)
        {
            if (!IsValid) return true;

            if (Operator == Operators.RegularExpression)
            {
                var textValue = item.GetText(ColumnName);
                if (textValue == null) return true;
                if (!IsValidText) return true;

                return Regex.IsMatch(textValue, FilterText, IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
            }

            if (Operator == Operators.Contains || Operator == Operators.DoesNotContain)
            {
                var textValue = item.GetText(ColumnName);
                if (textValue == null) return true;
                var result = false;
                if(IsCaseSensitive)
                {
                    result = textValue.Contains(FilterText);
                }
                else
                {
                    result = textValue.ToLower().Contains(FilterText.ToLower());
                }

                return Operator == Operators.DoesNotContain ? !result : result;
            }

            if (Operator == Operators.BeforeDate || Operator == Operators.AfterDate)
            {
                var textValue = item.GetText(ColumnName);
                if(!DateTimeOffset.TryParse(textValue, out var testDate))
                {
                    return true;
                }

                if(!_filterTextAsDateOffset.HasValue)
                {
                    return true;
                }

                return Operator == Operators.BeforeDate ?
                    testDate < _filterTextAsDateOffset :
                    testDate > _filterTextAsDateOffset;
            }


            if (IsValueColumn && double.TryParse(FilterText, out var filterValue))
            {
                var numericValue = item.GetValue(ColumnName);
                switch (Operator)
                {
                    case Operators.EqualTo: return numericValue == filterValue;
                    case Operators.NotEqualTo: return numericValue != filterValue;
                    case Operators.GreaterThan: return numericValue > filterValue;
                    case Operators.LessThan: return numericValue < filterValue;
                    default: throw new ApplicationException("Unhandled operator: " + Operator);
                }
            }
            else
            {
                var textValue = item.GetText(ColumnName);
                if (textValue == null) return true;
                var result = string.Compare(textValue, FilterText, !IsCaseSensitive);
                switch (Operator)
                {
                    case Operators.EqualTo: return result == 0;
                    case Operators.NotEqualTo: return result != 0;
                    case Operators.GreaterThan: return result > 0;
                    case Operators.LessThan: return result < 0;
                    default: throw new ApplicationException("Unhandled operator: " + Operator);
                }
            }


        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// ToString
        /// </summary>
        /// -----------------------------------------------------------------------
        public override string ToString()
        {
            return $"{ColumnName} {Operator} {FilterText}";
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Return a copy of this filter
        /// </summary>
        /// -----------------------------------------------------------------------
        internal TreeFilter Clone()
        {
            return new TreeFilter()
            {
                ColumnName = ColumnName,
                Id = Id,
                IsCaseSensitive = IsCaseSensitive,
                IsValueColumn = IsValueColumn,
                Operator = Operator,
                FilterText = FilterText,
            };
             
        }
    }
}
