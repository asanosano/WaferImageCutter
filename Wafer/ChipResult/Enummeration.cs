using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wafer.ChipResult
{
    public enum Divisions { Dummy = 0/*プロパティグリッドで追加するとき値０のものが必要*/, FrontRight = 1, FrontLeft, BackRight, BackLeft }
    public enum FilterInspectMethod { Black = 1, Clogging, Diameter, HoleCount }
    public enum CutAndInspectMethod { DCD, FilterDP, Measure, DeepLearning, DeepLearningCategorize, CutOnly, Symmetry, Template }
    public enum DefectLabel { フィルタ破れ = 1, 折れ, Ni突起, 異物 = 20, ブリッジ残り }
    public enum DefectGrade { None, OK, Gray, NG }
    public enum PositionDetectMethod { None, DLGenerator }
}
