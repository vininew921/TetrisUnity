﻿using System.Collections.Generic;
using System.Linq;
using Random = System.Random;

namespace Assets.Scripts
{
    public class Board
    {
        public int[][] Positions;
        public int Rows;
        public int Columns;
        public int Points;
        public int Level;

        public Tetrominoe currentPiece;
        public Tetrominoe phantomPiece;
        public Tetrominoe nextPiece;
        public Tetrominoe switchPiece;

        public bool Lost;
        public bool ClearedLines;
        public bool PieceLocked;

        private Random random;
        private Queue<int> pieceQueue;
        private int totalClearedLines;
        private bool canSwitch;

        private readonly int[] pointTable =
        {
            40,
            100,
            300,
            1200
        };

        public Board(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
            Points = 0;
            Level = 0;

            pieceQueue = new Queue<int>();
            random = new Random();
            canSwitch = true;

            GeneratePieceQueue();

            Lost = false;
            ClearedLines = false;
            PieceLocked = false;

            totalClearedLines = 0;

            Positions = new int[rows][];
            for (int y = 0; y < rows; y++)
            {
                Positions[y] = new int[columns];
                for (int x = 0; x < columns; x++)
                {
                    Positions[y][x] = 0;
                }
            }

            int currentPieceIndex = pieceQueue.Dequeue();
            int nextPieceIndex = pieceQueue.Peek();

            currentPiece = Tetrominoe.GetTetrominoe(currentPieceIndex);
            phantomPiece = Tetrominoe.GetTetrominoe(currentPieceIndex);
            nextPiece = Tetrominoe.GetTetrominoe(nextPieceIndex);

            SetPhantomPiece();
        }

        public void Update()
        {
            currentPiece.Y++;

            if (!ValidPosition(currentPiece))
            {
                currentPiece.Y--;
                GenerateNextPiece();
            }
        }

        public void Move(int x, int y)
        {
            currentPiece.X += x;
            currentPiece.Y += y;

            if (!ValidPosition(currentPiece, (x != 0 && y == 0)))
            {
                currentPiece.X -= x;
                currentPiece.Y -= y;

                if (!currentPiece.Active)
                {
                    GenerateNextPiece();
                }
            }

            SetPhantomPiece();
        }

        public void SwitchPiece()
        {
            if (canSwitch)
            {
                canSwitch = false;

                switchPiece ??= Tetrominoe.GetTetrominoe(pieceQueue.Dequeue());

                if (pieceQueue.Count == 0)
                {
                    GeneratePieceQueue();
                }

                int[][] tempShape = switchPiece.Shape;
                switchPiece.Shape = currentPiece.Shape;
                switchPiece.Y = 0;
                switchPiece.X = 3;
                if (ValidPosition(switchPiece))
                {
                    currentPiece.Shape = tempShape;
                    currentPiece.X = switchPiece.X;
                    currentPiece.Y = switchPiece.Y;
                    phantomPiece.Shape = tempShape;
                    SetPhantomPiece();
                }
            }
        }

        public void HardDrop()
        {
            while (ValidPosition(currentPiece))
            {
                currentPiece.Y++;
            }

            currentPiece.Y--;
            GenerateNextPiece();
        }

        public void Rotate(bool clockWise)
        {
            RotatePiece(currentPiece, clockWise);

            phantomPiece.X = currentPiece.X;
            phantomPiece.Y = currentPiece.Y;
            RotatePiece(phantomPiece, clockWise);
            SetPhantomPiece();
        }

        private void GeneratePieceQueue()
        {
            int[] values = { 0, 1, 2, 3, 4, 5, 6 };
            int[] randomValues = values.OrderBy(x => random.Next()).ToArray();

            for(int i = 0; i < randomValues.Length; i++)
            {
                pieceQueue.Enqueue(randomValues[i]);
            }
        }

        private void SetPhantomPiece()
        {
            phantomPiece.X = currentPiece.X;
            phantomPiece.Y = currentPiece.Y;

            while (ValidPosition(phantomPiece))
            {
                phantomPiece.Y++;
            }

            phantomPiece.Y--;
            phantomPiece.X = currentPiece.X;
        }

        private void GenerateNextPiece()
        {
            FreezeBoard();
            PieceLocked = true;
            canSwitch = true;

            int currentPieceIndex = pieceQueue.Dequeue();

            currentPiece = Tetrominoe.GetTetrominoe(currentPieceIndex);

            int clearedLines = ClearLines();
            if (clearedLines > 0)
            {
                totalClearedLines += clearedLines;
                Points += pointTable[clearedLines - 1];
                ClearedLines = true;

                Level = totalClearedLines / 10;
            }

            phantomPiece = Tetrominoe.GetTetrominoe(currentPieceIndex);

            SetPhantomPiece();

            if (pieceQueue.Count == 0)
            {
                GeneratePieceQueue();
            }

            int nextPieceIndex = pieceQueue.Peek();
            nextPiece = Tetrominoe.GetTetrominoe(nextPieceIndex);

            if (!ValidPosition(currentPiece))
            {
                Lost = true;
            }
        }

        private int ClearLines()
        {
            int clearedLines = 0;

            for (int y = 0; y < Rows; y++)
            {
                bool clear = true;
                for (int x = 0; x < Columns; x++)
                {
                    if (Positions[y][x] == 0)
                    {
                        clear = false;
                        break;
                    }
                }

                if (clear)
                {
                    clearedLines++;
                    for (int clearY = y; clearY >= 0; clearY--)
                    {
                        for (int x = 0; x < Columns; x++)
                        {
                            if (clearY == 0)
                            {
                                Positions[0][x] = 0;
                            }
                            else
                            {
                                Positions[clearY][x] = Positions[clearY - 1][x];
                            }
                        }
                    }
                }
            }

            return clearedLines;
        }

        private void RotatePiece(Tetrominoe piece, bool clockWise)
        {
            int[][] newShape = new int[piece.Shape.Length][];

            if (clockWise)
            {
                //Transpose
                for (int y = 0; y < piece.Shape.Length; y++)
                {
                    newShape[y] = new int[piece.Shape[y].Length];
                    for (int x = 0; x < piece.Shape[y].Length; x++)
                    {
                        newShape[y][x] = piece.Shape[x][y];
                    }
                }

                //Reverse rows
                for (int y = 0; y < piece.Shape.Length; y++)
                {
                    newShape[y] = newShape[y].Reverse().ToArray();
                }
            }
            else
            {
                int[][] reversed = new int[piece.Shape.Length][];

                //Reverse rows
                for (int y = 0; y < piece.Shape.Length; y++)
                {
                    reversed[y] = piece.Shape[y].Reverse().ToArray();
                }

                //Transpose
                for (int y = 0; y < piece.Shape.Length; y++)
                {
                    newShape[y] = new int[piece.Shape[y].Length];
                    for (int x = 0; x < piece.Shape[y].Length; x++)
                    {
                        newShape[y][x] = reversed[x][y];
                    }
                }
            }

            int[][] oldShape = piece.Shape;
            piece.Shape = newShape;

            if (!ValidPosition(piece))
            {
                piece.Shape = oldShape;
            }
        }

        private void FreezeBoard()
        {
            for (int y = 0; y < currentPiece.Shape.Length; y++)
            {
                for (int x = 0; x < currentPiece.Shape[y].Length; x++)
                {
                    if (currentPiece.Shape[y][x] != 0)
                    {
                        Positions[y + currentPiece.Y][x + currentPiece.X] = currentPiece.Shape[y][x];
                    }
                }
            }
        }

        private bool ValidPosition(Tetrominoe piece, bool horizontalMovement = false)
        {
            try
            {
                for (int y = 0; y < piece.Shape.Length; y++)
                {
                    for (int x = 0; x < piece.Shape[y].Length; x++)
                    {
                        if (piece.Shape[y][x] != 0)
                        {
                            if (x + piece.X < 0 || x + piece.X >= Columns)
                            {
                                return false;
                            }

                            if (y + piece.Y >= Rows || Positions[y + piece.Y][x + piece.X] != 0)
                            {
                                piece.Active = horizontalMovement;
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
