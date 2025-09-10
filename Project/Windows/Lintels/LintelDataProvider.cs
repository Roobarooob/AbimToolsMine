using System;
using System.Collections.Generic;
using System.Linq;

namespace AbimToolsMine
{
    public static class LintelDataProvider
    {
        // «‡¯ËÚ˚Â ‰‡ÌÌ˚Â ·ÛÒÍÓ‚˚ı ˝ÎÂÏÂÌÚÓ‚
        private static readonly List<string> BruskElementsData = new List<string>
        {
            "1œ¡ 10-1", "1œ¡ 13-1", "1œ¡ 16-1", "2œ¡ 10-1", "2œ¡ 10-1-Ô", "2œ¡ 13-1", "2œ¡ 13-1-Ô", "2œ¡ 16-2", "2œ¡ 16-2-Ô", "2œ¡ 17-2", "2œ¡ 17-2-Ô", "2œ¡ 19-3", "2œ¡ 19-3-Ô", "2œ¡ 22-3", "2œ¡ 22-3-Ô", "2œ¡ 25-3", "2œ¡ 25-3-Ô", "2œ¡ 26-4", "2œ¡ 26-4-Ô", "2œ¡ 29-4", "2œ¡ 29-4-Ô", "2œ¡ 30-4", "2œ¡ 30-4-Ô", "3œ¡ 18-8", "3œ¡ 18-8-Ô", "3œ¡ 21-8", "3œ¡ 21-8-Ô", "3œ¡ 25-8", "3œ¡ 25-8-Ô", "3œ¡ 27-8", "3œ¡ 27-8-Ô", "3œ¡ 30-8", "3œ¡ 30-8-Ô", "3œ¡ 34-4", "3œ¡ 34-4-Ô", "3œ¡ 36-4", "3œ¡ 36-4-Ô", "3œ¡ 39-8", "3œ¡ 39-8-Ô", "3œ¡ 13-37", "3œ¡ 13-37-Ô", "3œ¡ 16-37", "3œ¡ 16-37-Ô", "3œ¡ 18-37", "3œ¡ 18-37-Ô", "4œ¡ 30-4", "4œ¡ 30-4-Ô", "4œ¡ 44-8", "4œ¡ 44-8-Ô", "4œ¡ 48-8", "4œ¡ 48-8-Ô", "4œ¡ 60-8", "4œ¡ 60-8-Ô",
            "5œ¡ 18-27", "5œ¡ 18-27-Ô", "5œ¡ 21-27", "5œ¡ 21-27-Ô", "5œ¡ 25-37", "5œ¡ 25-37-Ô", "5œ¡ 25-27", "5œ¡ 25-27-Ô", "5œ¡ 27-37", "5œ¡ 27-37-Ô", "5œ¡ 27-27", "5œ¡ 27-27-Ô", "5œ¡ 30-37", "5œ¡ 30-37-Ô", "5œ¡ 30-27", "5œ¡ 30-27-Ô", "5œ¡ 31-27", "5œ¡ 31-27-Ô", "5œ¡ 34-20", "5œ¡ 34-20-Ô", "5œ¡ 36-20", "5œ¡ 36-20-Ô", "5œ¡ 21-27-‡", "5œ¡ 21-27-‡Ô", "5œ¡ 25-27-‡", "5œ¡ 25-27-‡Ô", "5œ¡ 27-27-‡", "5œ¡ 27-27-‡Ô", "5œ¡ 30-27-‡", "5œ¡ 30-27-‡Ô",
            "1œœ 12-3", "4œœ 12-4", "2œœ 14-4", "2œœ 17-5", "2œœ 18-5", "2œœ 21-6", "2œœ 23-7", "2œœ 25-8", "5œœ 14-5", "5œœ 17-6", "5œœ 23-10", "6œœ 30-13", "3œœ 14-71", "3œœ 16-71", "3œœ 18-71", "3œœ 21-71", "3œœ 27-71", "3œœ 30-10",
            "8œ¡ 10-1", "8œ¡ 13-1", "8œ¡ 16-1", "8œ¡ 17-2", "8œ¡ 19-3", "9œ¡ 22-3", "9œ¡ 22-3-Ô", "9œ¡ 25-3", "9œ¡ 25-3-Ô", "9œ¡ 26-4", "9œ¡ 26-4-Ô", "9œ¡ 29-4", "9œ¡ 29-4-Ô", "9œ¡ 30-4", "9œ¡ 30-4-Ô", "9œ¡ 13-37", "9œ¡ 13-37-Ô", "9œ¡ 16-37", "9œ¡ 16-37-Ô", "9œ¡ 18-37", "9œ¡ 18-37-Ô", "9œ¡ 18-8", "9œ¡ 18-8-Ô", "9œ¡ 21-8", "9œ¡ 21-8-Ô", "9œ¡ 25-8", "9œ¡ 25-8-Ô", "9œ¡ 27-8", "9œ¡ 27-8-Ô",
            "10œ¡ 18-27", "10œ¡ 18-27-Ô", "10œ¡ 21-27", "10œ¡ 21-27-Ô", "10œ¡ 25-37", "10œ¡ 25-37-Ô", "10œ¡ 25-27", "10œ¡ 25-27-Ô", "10œ¡ 27-37", "10œ¡ 27-37-Ô", "10œ¡ 27-27", "10œ¡ 27-27-Ô", "10œ¡ 21-27-‡", "10œ¡ 21-27-‡Ô", "10œ¡ 25-27-‡", "10œ¡ 25-27-‡Ô", "10œ¡ 27-27-‡", "10œ¡ 27-27-‡Ô",
            "7œœ 12-3", "7œœ 14-4", "9œœ 12-4", "9œœ 14-5", "9œœ 17-6", "8œœ 17-5", "8œœ 18-5", "8œœ 21-6", "8œœ 23-7", "8œœ 25-8", "8œœ 30-10", "10œœ 23-10", "10œœ 30-13", "8œœ 14-71", "8œœ 16-71", "8œœ 18-71", "8œœ 21-71", "8œœ 27-71"
        };

        // «‡¯ËÚ˚Â ‰‡ÌÌ˚Â Û„ÓÎÍÓ‚
        private static readonly List<string> UgolkElementsData = new List<string>
        {
            "20x3", "20x4", "25x3", "25x4", "28x3", "30x3", "30x4", "32x3", "32x4", "35x3", "35x4", "35x5", "40x3", "40x4", "40x5",
            "45x3", "45x4", "45x5", "50x3", "50x4", "50x5", "50x6", "56x4", "56x5", "63x4", "63x5", "63x6", "70x4.5", "70x5", "70x6",
            "70x7", "70x8", "75x5", "75x6", "75x7", "75x8", "75x9", "80x5.5", "80x6", "80x7", "80x8", "90x6", "90x7", "90x8", "90x9",
            "100x6.5", "100x7", "100x8", "100x10", "100x12", "100x14", "100x16", "110x7", "110x8", "125x8", "125x9", "125x10", "125x12",
            "125x14", "125x16", "140x9", "140x10", "140x12", "160x10", "160x11", "160x12", "160x14", "160x16", "160x18", "160x20",
            "180x11", "180x12", "200x12", "200x13", "200x14", "200x16", "200x20", "200x25", "200x30", "220x14", "220x16", "250x16",
            "250x18", "250x20", "250x22", "250x25", "250x28", "250x30", "250x35"
        };

        // «‡¯ËÚ˚Â ‰‡ÌÌ˚Â ÔÓÎÓÒ
        private static readonly List<string> PolosaElementsData = new List<string>
        {
            "5x10", "4x12", "5x12", "6x12", "8x12", "6x14", "8x14", "5x15", "6x15", "8x15", "10x15", "4x16", "5x16", "6x16", "7x16", "8x16",
            "9x16", "10x16", "11x16", "12x16", "14x16", "4x18", "5x18", "6x18", "7x18", "8x18", "9x18", "10x18", "11x18", "12x18", "14x18",
            "16x18", "4x20", "5x20", "6x20", "7x20", "8x20", "9x20", "10x20", "11x20", "12x20", "14x20", "15x20", "16x20", "4x22", "5x22",
            "6x22", "7x22", "8x22", "9x22", "10x22", "11x22", "12x22", "14x22", "16x22", "18x22", "20x22", "4x25", "5x25", "6x25", "7x25",
            "8x25", "9x25", "10x25", "11x25", "12x25", "14x25", "15x25", "16x25", "18x25", "20x25", "22x25", "4x28", "5x28", "6x28", "7x28",
            "8x28", "9x28", "10x28", "11x28", "12x28", "14x28", "16x28", "18x28", "20x28", "22x28", "25x28", "4x30", "5x30", "6x30", "7x30",
            "8x30", "9x30", "10x30", "11x30", "12x30", "14x30", "15x30", "16x30", "18x30", "20x30", "22x30", "4x32", "5x32", "6x32", "7x32",
            "8x32", "9x32", "10x32", "11x32", "12x32", "14x32", "16x32", "18x32", "20x32", "22x32", "25x32", "28x32", "30x32", "4x35", "5x35",
            "6x35", "8x35", "10x35", "12x35", "15x35", "20x35", "25x35", "30x35", "4x36", "5x36", "6x36", "7x36", "8x36", "9x36", "10x36",
            "11x36", "12x36", "14x36", "16x36", "18x36", "20x36", "22x36", "25x36", "28x36", "30x36", "4x40", "5x40", "6x40", "7x40", "8x40",
            "9x40", "10x40", "11x40", "12x40", "14x40", "15x40", "16x40", "18x40", "20x40", "22x40", "25x40", "28x40", "30x40", "32x40",
            "4x45", "5x45", "6x45", "7x45", "8x45", "9x45", "10x45", "11x45", "12x45", "14x45", "15x45", "16x45", "18x45", "20x45", "22x45",
            "25x45", "28x45", "30x45", "32x45", "36x45", "4x50", "5x50", "6x50", "7x50", "8x50", "9x50", "10x50", "11x50", "12x50", "14x50",
            "15x50", "16x50", "18x50", "20x50", "22x50", "25x50", "28x50", "30x50", "32x50", "36x50", "4x55", "5x55", "6x55", "7x55", "8x55",
            "9x55", "10x55", "11x55", "12x55", "14x55", "16x55", "18x55", "20x55", "22x55", "25x55", "28x55", "30x55", "32x55", "36x55",
            "5x60", "6x60", "7x60", "8x60", "9x60", "10x60", "11x60", "12x60", "14x60", "15x60", "16x60", "18x60", "20x60", "22x60", "25x60",
            "28x60", "30x60", "32x60", "35x60", "36x60", "40x60", "45x60", "50x60", "56x60", "6x63", "5x65", "6x65", "7x65", "8x65", "9x65",
            "10x65", "11x65", "12x65", "14x65", "15x65", "16x65", "18x65", "20x65", "22x65", "25x65", "28x65", "30x65", "32x65", "36x65",
            "40x65", "45x65", "50x65", "56x65", "60x65", "5x70", "6x70", "7x70", "8x70", "9x70", "10x70", "11x70", "12x70", "14x70", "15x70",
            "16x70", "18x70", "20x70", "22x70", "25x70", "28x70", "30x70", "32x70", "35x70", "36x70", "40x70", "45x70", "50x70", "56x70",
            "60x70", "4x75", "5x75", "6x75", "7x75", "8x75", "9x75", "10x75", "11x75", "12x75", "14x75", "15x75", "16x75", "18x75", "20x75",
            "22x75", "25x75", "28x75", "30x75", "32x75", "36x75", "40x75", "45x75", "50x75", "56x75", "60x75", "5x80", "6x80", "7x80", "8x80",
            "9x80", "10x80", "11x80", "12x80", "14x80", "15x80", "16x80", "18x80", "20x80", "22x80", "25x80", "28x80", "30x80", "32x80",
            "35x80", "36x80", "40x80", "45x80", "50x80", "56x80", "60x80", "6x85", "7x85", "8x85", "9x85", "10x85", "11x85", "12x85", "14x85",
            "16x85", "18x85", "20x85", "22x85", "25x85", "28x85", "30x85", "32x85", "36x85", "40x85", "45x85", "50x85", "56x85", "60x85",
            "5x90", "6x90", "7x90", "8x90", "9x90", "10x90", "11x90", "12x90", "14x90", "15x90", "16x90", "18x90", "20x90", "22x90", "25x90",
            "28x90", "30x90", "32x90", "35x90", "36x90", "40x90", "45x90", "50x90", "56x90", "60x90", "6x95", "7x95", "8x95", "9x95", "10x95",
            "11x95", "12x95", "14x95", "16x95", "18x95", "20x95", "22x95", "25x95", "5x100", "6x100", "7x100", "8x100", "9x100", "10x100",
            "11x100", "12x100", "14x100", "15x100", "16x100", "18x100", "20x100", "22x100", "25x100", "30x100", "35x100", "40x100", "50x100",
            "60x100", "6x105", "7x105", "8x105", "9x105", "22x105", "25x105", "6x110", "7x110", "8x110", "9x110", "12x110", "14x110", "22x110",
            "25x110", "6x120", "7x120", "8x120", "10x120", "12x120", "14x120", "15x120", "20x120", "22x120", "25x120", "30x120", "35x120",
            "40x120", "50x120", "6x125", "7x125", "8x125", "22x125", "25x125", "6x130", "7x130", "8x130", "9x130", "10x130", "12x130",
            "14x130", "15x130", "20x130", "22x130", "25x130", "30x130", "40x130", "50x130", "6x140", "7x140", "8x140", "10x140", "12x140",
            "14x140", "15x140", "16x140", "18x140", "20x140", "22x140", "25x140", "30x140", "40x140", "50x140", "6x150", "7x150", "8x150",
            "9x150", "10x150", "11x150", "12x150", "14x150", "15x150", "16x150", "18x150", "20x150", "22x150", "25x150", "28x150", "30x150",
            "32x150", "35x150", "36x150", "40x150", "45x150", "50x150", "56x150", "60x150", "80x150", "6x160", "7x160", "8x160", "9x160",
            "10x160", "11x160", "12x160", "14x160", "16x160", "18x160", "20x160", "22x160", "25x160", "28x160", "30x160", "32x160", "36x160",
            "40x160", "45x160", "50x160", "56x160", "60x160"
        };

        // «‡¯ËÚ˚Â ‰‡ÌÌ˚Â ‡Ï‡ÚÛ˚
        private static readonly List<ArmaturaData> ArmaturaElementsData = new List<ArmaturaData>
        {
            // ¿240
            new ArmaturaData { Diameter = 4.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 4.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 5.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 5.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 6.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 6.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 7.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 7.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 8.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 8.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 9.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 9.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 10.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 11.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 12.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 13.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 14.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 15.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 16.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 17.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 18.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 19.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 20.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 22.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 25.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 28.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 32.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 36.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },
            new ArmaturaData { Diameter = 40.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿240" },

            // ¿400
            new ArmaturaData { Diameter = 4.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 4.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 5.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 5.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 6.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 6.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 7.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 7.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 8.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 8.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 9.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 9.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 10.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 11.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 12.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 13.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 14.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 15.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 16.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 18.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 20.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 22.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 25.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 28.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 32.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 36.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },
            new ArmaturaData { Diameter = 40.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿400" },

            // ¿500
            new ArmaturaData { Diameter = 4.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 4.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 5.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 5.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 6.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 6.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 7.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 7.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 8.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 8.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 9.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 9.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 10.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 11.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 12.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 13.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 14.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 15.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 16.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 18.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 20.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 22.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 25.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 28.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 32.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 36.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },
            new ArmaturaData { Diameter = 40.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿500" },

            // ¿600
            new ArmaturaData { Diameter = 4.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 4.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 5.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 5.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 6.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 6.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 7.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 7.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 8.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 8.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 9.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 9.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 10.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 11.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 12.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 13.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 14.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 15.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 16.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 18.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 20.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 22.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 25.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 28.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 32.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 36.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },
            new ArmaturaData { Diameter = 40.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿600" },

            // ¿800
            new ArmaturaData { Diameter = 4.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 4.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 5.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 5.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 6.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 6.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 7.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 7.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 8.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 8.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 9.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 9.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 10.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 11.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 12.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 13.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 14.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 15.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 16.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 18.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 20.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 22.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 25.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 28.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 32.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 36.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },
            new ArmaturaData { Diameter = 40.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿800" },

            // ¿1000
            new ArmaturaData { Diameter = 4.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 4.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 5.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 5.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 6.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 6.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 7.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 7.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 8.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 8.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 9.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 9.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 10.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 11.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 12.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 13.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 14.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 15.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 16.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 18.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 20.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 22.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 25.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 28.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 32.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 36.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },
            new ArmaturaData { Diameter = 40.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿1000" },

            // ¿Ô600
            new ArmaturaData { Diameter = 4.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 4.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 5.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 5.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 6.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 6.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 7.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 7.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 8.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 8.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 9.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 9.5, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 10.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 11.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 12.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 13.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 14.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 15.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 16.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 18.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 20.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 22.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 25.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 28.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 32.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 36.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },
            new ArmaturaData { Diameter = 40.0, Gost = "√Œ—“ 34028-2016", ClassName = "¿Ô600" },

            // ¬500
            new ArmaturaData { Diameter = 4.0, Gost = "√Œ—“ – 52544-2006", ClassName = "¬500" },
            new ArmaturaData { Diameter = 4.5, Gost = "√Œ—“ – 52544-2006", ClassName = "¬500" },
            new ArmaturaData { Diameter = 5.0, Gost = "√Œ—“ – 52544-2006", ClassName = "¬500" },
            new ArmaturaData { Diameter = 5.5, Gost = "√Œ—“ – 52544-2006", ClassName = "¬500" },
            new ArmaturaData { Diameter = 6.0, Gost = "√Œ—“ – 52544-2006", ClassName = "¬500" },
            new ArmaturaData { Diameter = 6.5, Gost = "√Œ—“ – 52544-2006", ClassName = "¬500" },
            new ArmaturaData { Diameter = 7.0, Gost = "√Œ—“ – 52544-2006", ClassName = "¬500" },
            new ArmaturaData { Diameter = 7.5, Gost = "√Œ—“ – 52544-2006", ClassName = "¬500" },
            new ArmaturaData { Diameter = 8.0, Gost = "√Œ—“ – 52544-2006", ClassName = "¬500" },
            new ArmaturaData { Diameter = 8.5, Gost = "√Œ—“ – 52544-2006", ClassName = "¬500" },
            new ArmaturaData { Diameter = 9.0, Gost = "√Œ—“ – 52544-2006", ClassName = "¬500" },
            new ArmaturaData { Diameter = 9.5, Gost = "√Œ—“ – 52544-2006", ClassName = "¬500" },
            new ArmaturaData { Diameter = 10.0, Gost = "√Œ—“ – 52544-2006", ClassName = "¬500" },
            new ArmaturaData { Diameter = 12.0, Gost = "√Œ—“ – 52544-2006", ClassName = "¬500" },

            // ¬-II
            new ArmaturaData { Diameter = 3.0, Gost = "√Œ—“ 7348-81", ClassName = "¬-II" },
            new ArmaturaData { Diameter = 4.0, Gost = "√Œ—“ 7348-81", ClassName = "¬-II" },
            new ArmaturaData { Diameter = 5.0, Gost = "√Œ—“ 7348-81", ClassName = "¬-II" },
            new ArmaturaData { Diameter = 6.0, Gost = "√Œ—“ 7348-81", ClassName = "¬-II" },
            new ArmaturaData { Diameter = 7.0, Gost = "√Œ—“ 7348-81", ClassName = "¬-II" },
            new ArmaturaData { Diameter = 8.0, Gost = "√Œ—“ 7348-81", ClassName = "¬-II" },

            // ¬p-II
            new ArmaturaData { Diameter = 3.0, Gost = "√Œ—“ 7348-81", ClassName = "¬p-II" },
            new ArmaturaData { Diameter = 4.0, Gost = "√Œ—“ 7348-81", ClassName = "¬p-II" },
            new ArmaturaData { Diameter = 5.0, Gost = "√Œ—“ 7348-81", ClassName = "¬p-II" },
            new ArmaturaData { Diameter = 6.0, Gost = "√Œ—“ 7348-81", ClassName = "¬p-II" },
            new ArmaturaData { Diameter = 7.0, Gost = "√Œ—“ 7348-81", ClassName = "¬p-II" },
            new ArmaturaData { Diameter = 8.0, Gost = "√Œ—“ 7348-81", ClassName = "¬p-II" },

            // ¬p-I
            new ArmaturaData { Diameter = 3.0, Gost = "√Œ—“ 6727-80", ClassName = "¬p-I" },
            new ArmaturaData { Diameter = 4.0, Gost = "√Œ—“ 6727-80", ClassName = "¬p-I" },
            new ArmaturaData { Diameter = 5.0, Gost = "√Œ—“ 6727-80", ClassName = "¬p-I" }
        };

        /// <summary>
        /// ¬ÓÁ‚‡˘‡ÂÚ ÒÔËÒÓÍ ·ÛÒÍÓ‚˚ı ˝ÎÂÏÂÌÚÓ‚
        /// </summary>
        public static List<string> GetBruskElements()
        {
            return new List<string>(BruskElementsData);
        }

        /// <summary>
        /// ¬ÓÁ‚‡˘‡ÂÚ ÒÔËÒÓÍ ÚËÔÓ‚ Û„ÓÎÍÓ‚
        /// </summary>
        public static List<string> GetUgolkElements()
        {
            return new List<string>(UgolkElementsData);
        }

        /// <summary>
        /// ¬ÓÁ‚‡˘‡ÂÚ ÒÔËÒÓÍ ÚËÔÓ‚ ÔÓÎÓÒ
        /// </summary>
        public static List<string> GetPolosaElements()
        {
            return new List<string>(PolosaElementsData);
        }

        /// <summary>
        /// ¬ÓÁ‚‡˘‡ÂÚ ÒÔËÒÓÍ ‚ÒÂı ˝ÎÂÏÂÌÚÓ‚ ‡Ï‡ÚÛ˚
        /// </summary>
        public static List<ArmaturaData> GetArmaturaElements()
        {
            return new List<ArmaturaData>(ArmaturaElementsData);
        }

        /// <summary>
        /// ¬ÓÁ‚‡˘‡ÂÚ ÒÔËÒÓÍ ÛÌËÍ‡Î¸Ì˚ı √Œ—“ ‰Îˇ ‡Ï‡ÚÛ˚
        /// </summary>
        public static List<string> GetArmaturaGosts()
        {
            return GetArmaturaElements()
                .Select(a => a.Gost)
                .Distinct()
                .OrderBy(g => g)
                .ToList();
        }

        /// <summary>
        /// ¬ÓÁ‚‡˘‡ÂÚ ÒÔËÒÓÍ ‰Ë‡ÏÂÚÓ‚ ‡Ï‡ÚÛ˚ ‰Îˇ Á‡‰‡ÌÌÓ„Ó √Œ—“
        /// </summary>
        public static List<double> GetArmaturaDiameters(string gost)
        {
            return GetArmaturaElements()
                .Where(a => a.Gost == gost)
                .Select(a => a.Diameter)
                .Distinct()
                .OrderBy(d => d)
                .ToList();
        }

        /// <summary>
        /// ¬ÓÁ‚‡˘‡ÂÚ ÒÔËÒÓÍ ÍÎ‡ÒÒÓ‚ ‡Ï‡ÚÛ˚ ‰Îˇ Á‡‰‡ÌÌÓ„Ó √Œ—“ Ë ‰Ë‡ÏÂÚ‡
        /// </summary>
        public static List<string> GetArmaturaClasses(string gost, double diameter)
        {
            return GetArmaturaElements()
                .Where(a => a.Gost == gost && Math.Abs(a.Diameter - diameter) < 0.01)
                .Select(a => a.ClassName)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }

        /// <summary>
        /// ¬ÓÁ‚‡˘‡ÂÚ ÒÔËÒÓÍ ÛÌËÍ‡Î¸Ì˚ı ÍÎ‡ÒÒÓ‚ ‡Ï‡ÚÛ˚ ‰Îˇ Á‡‰‡ÌÌÓ„Ó √Œ—“
        /// </summary>
        public static List<string> GetArmaturaClassesByGost(string gost)
        {
            return GetArmaturaElements()
                .Where(a => a.Gost == gost)
                .Select(a => a.ClassName)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }

        /// <summary>
        /// ¬ÓÁ‚‡˘‡ÂÚ ÒÔËÒÓÍ ‰Ë‡ÏÂÚÓ‚ ‡Ï‡ÚÛ˚ ‰Îˇ Á‡‰‡ÌÌÓ„Ó √Œ—“ Ë ÍÎ‡ÒÒ‡
        /// </summary>
        public static List<double> GetArmaturaDiametersByGostAndClass(string gost, string className)
        {
            return GetArmaturaElements()
                .Where(a => a.Gost == gost && a.ClassName == className)
                .Select(a => a.Diameter)
                .Distinct()
                .OrderBy(d => d)
                .ToList();
        }
    }

    /// <summary>
    ///  Î‡ÒÒ ‰Îˇ ı‡ÌÂÌËˇ ‰‡ÌÌ˚ı Ó· ‡Ï‡ÚÛÂ
    /// </summary>
    public class ArmaturaData
    {
        public double Diameter { get; set; }
        public string Gost { get; set; }
        public string ClassName { get; set; }
    }

    /// <summary>
    ///  ÓÌÙË„Û‡ˆËˇ ÔÂÂÏ˚˜ÍË
    /// </summary>
    public class LintelConfig
    {
        public string LintelType { get; set; }
        public List<string> BrusElements { get; set; }
        public LintelUgolkConfig UgolkConfig { get; set; }
        public LintelArmaturaConfig ArmaturaConfig { get; set; }
    }

    /// <summary>
    ///  ÓÌÙË„Û‡ˆËˇ Û„ÓÎÍÓ‚ ‰Îˇ ÔÂÂÏ˚˜ÍË
    /// </summary>
    public class LintelUgolkConfig
    {
        public string Step { get; set; }
        public string Offset { get; set; }
        public string UgolkType { get; set; }
        public string StripType { get; set; }
    }

    /// <summary>
    ///  ÓÌÙË„Û‡ˆËˇ ‡Ï‡ÚÛ˚ ‰Îˇ ÔÂÂÏ˚˜ÍË
    /// </summary>
    public class LintelArmaturaConfig
    {
        public int Count { get; set; }
        public string Gost { get; set; }
        public double Diameter { get; set; }
        public string ClassName { get; set; }
    }
}