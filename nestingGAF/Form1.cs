using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GAF;

namespace nestingGAF
{
    public partial class Form1 : Form
    {
        private Node root;
        private List<RectangleF> rectangles = new List<RectangleF>();

        private float panelWidth = 200;
        private float panelHeight = 200;

        private float boxHeight = 50;
        private float boxLength = 100;
        private float boxWidth = 30;

        private float rectangleOffset = 5;

        public Form1()
        {
            InitializeComponent();
            panel1.Size = new Size((int)panelWidth, (int)panelHeight);
            GenerateRectangles(boxHeight, boxLength, boxWidth);
            InitializeRootNode();
            FitRectanglesInPanelWithGAF();
            panel1.Refresh();
        }

        private void GenerateRectangles(float boxHeight, float boxLength, float boxWidth)
        {
            rectangles.Add(new RectangleF(10, 10, boxWidth, boxHeight));
            rectangles.Add(new RectangleF(10, 10, boxWidth, boxHeight));
            rectangles.Add(new RectangleF(10, 10, boxLength, boxHeight));
            rectangles.Add(new RectangleF(10, 10, boxLength, boxHeight));
            rectangles.Add(new RectangleF(10, 10, boxLength, boxWidth));
            rectangles.Add(new RectangleF(10, 10, boxLength, boxWidth));
        }

        private void FitRectanglesInPanelWithGAF()
        {
            // Convert rectangles to the chromosome representation
            var chromosome = new GAF.Chromosome();
            foreach (var rect in rectangles)
            {
                int combinedValue = ((int)rect.Width << 16) | (int)rect.Height;
                chromosome.Genes.Add(new GAF.Gene(combinedValue));
            }

            // Create the population with a single chromosome
            var population = new GAF.Population();
            population.Solutions.Add(chromosome);

            FitnessFunction fitnessFunction = (x) =>
            {
                // Calculate the total area of the rectangles in the chromosome
                double totalArea = x.Genes.Sum(gene => (double)(((int)gene.RealValue) >> 16) * (double)(((int)gene.RealValue) & 0xFFFF));

                // Calculate the unused area in the panel
                double unusedArea = panelWidth * panelHeight - totalArea;

                // Normalize the fitness value to be within the range of 0.0 to 1.0
                double maxFitnessValue = panelWidth * panelHeight;
                double normalizedFitnessValue = 1.0 - (unusedArea / maxFitnessValue); // Invert the fitness value to minimize the unused area

                // The fitness value is the normalized unused area (minimize the unused area)
                return normalizedFitnessValue;
            };

            // Create the genetic algorithm with fitness function
            var ga = new GAF.GeneticAlgorithm(population, fitnessFunction);

            // Run the genetic algorithm for a few generations
            ga.Run(Terminate);

            // Retrieve the best solution after running the genetic algorithm
            var bestChromosome = ga.Population.GetTop(1)[0];

            // Update the rectangles with the best solution
            for (int i = 0; i < rectangles.Count; i++)
            {
                var gene = bestChromosome.Genes[i];
                int combinedValue = (int)gene.RealValue;
                int width = (combinedValue >> 16);
                int height = (combinedValue & 0xFFFF);

                // Create a temporary modified rectangle
                var rectangle = new RectangleF(rectangles[i].X, rectangles[i].Y, width, height);

                // Check if the rectangle is rotated
                var rotated = FindNode(root, height + rectangleOffset, width + rectangleOffset);

                if (rotated != null)
                {
                    // Rectangle can be rotated, swap width and height
                    rectangle.Width = height;
                    rectangle.Height = width;
                }

                // Pack the temporary rectangle using the Rectangle Packing algorithm with the specified offset
                Node packedNode = FindNode(root, rectangle.Width + rectangleOffset, rectangle.Height + rectangleOffset);

                if (packedNode != null)
                {
                    rectangle.X = packedNode.X;
                    rectangle.Y = packedNode.Y;

                    SplitNode(packedNode, rectangle.Width + rectangleOffset, rectangle.Height + rectangleOffset);

                    // Update the original rectangle with the packed position
                    rectangles[i] = rectangle;
                }
                else
                {
                    MessageBox.Show("Packing failed. Please adjust panel size or offset value.");
                    return;
                }
            }
            panel1.Invalidate();
        }

        private void InitializeRootNode()
        {
            root = new Node
            {
                X = 0,
                Y = 0,
                Width = panelWidth,
                Height = panelHeight,
                IsOccupied = false
            };
        }

        private bool Terminate(Population population, int currentGeneration, long currentEvaluation)
        {
            return currentGeneration >= 100; // Stop after 100 generations 
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Color.Black, 2))
            {
                foreach (var rectangle in rectangles)
                {
                    e.Graphics.DrawRectangle(pen, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
                }
            }
        }

        internal class Node
        {
            public Node RightNode { get; set; }
            public Node BottomNode { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float Width { get; set; }
            public float Height { get; set; }
            public bool IsOccupied { get; set; }
        }

        // Helper function to find the best node for packing the rectangle
        private Node FindNode(Node node, float width, float height)
        {
            if (node.IsOccupied)
            {
                Node rightNode = FindNode(node.RightNode, width, height);
                return rightNode ?? FindNode(node.BottomNode, width, height);
            }
            else if (width <= node.Width && height <= node.Height)
            {
                return node;
            }
            else
            {
                return null;
            }
        }

        // Helper function to split the packing area after packing a rectangle with the specified offset
        private void SplitNode(Node node, float width, float height)
        {
            node.IsOccupied = true;

            node.RightNode = new Node
            {
                X = node.X + width,
                Y = node.Y,
                Width = node.Width - width,
                Height = height
            };

            node.BottomNode = new Node
            {
                X = node.X,
                Y = node.Y + height,
                Width = node.Width,
                Height = node.Height - height
            };
        }
    }
}
