﻿using FunnyRouletteConsole.Physics;
using FunnyRouletteConsole.Rendering;

namespace FunnyRouletteConsole.Core
{
    /// <summary>
    /// Represents the ball in the roulette simulation.
    /// </summary>
    public class Ball : IPhysicsObject, IRenderable
    {
        // Angular position of the ball in degrees
        public double Position { get; set; }
        public double AngularVelocity { get; set; }

        public double InitialAngularVelocity { get; set; }
        public double DecayCoefficient { get; set; } = 0.3; // Adjusted for realistic spin duration
        public double StoppingThreshold { get; set; } = 5.0;
        public double DropThreshold { get; set; } = 100.0;
        public bool IsStopped { get; private set; } = false;

        // Physical properties
        public double Mass { get; set; } = 0.05; // In kilograms
        public double Radius { get; set; } = 0.02; // Ball radius (normalized units)
        public double BallRadius { get; set; } = 0.9; // Radius of ball's path (normalized units)
        public double MomentOfInertia => 0.4 * Mass * Radius * Radius; // Solid sphere

        private RouletteWheel _wheel;
        private double elapsedTime = 0;
        private bool hasDropped = false;

        /// <summary>
        /// Initializes a new instance of the Ball class.
        /// </summary>
        public Ball(RouletteWheel wheel)
        {
            _wheel = wheel;
        }

        /// <summary>
        /// Updates the ball's position and velocity.
        /// </summary>
        public void Update(double deltaTime)
        {
            if (IsStopped)
                return;

            elapsedTime += deltaTime;

            // Update angular velocity using exponential decay
            AngularVelocity = InitialAngularVelocity * Math.Exp(-DecayCoefficient * elapsedTime);

            // Simulate ball dropping into the pockets
            if (!hasDropped && AngularVelocity <= DropThreshold)
            {
                hasDropped = true;
                Console.WriteLine("[Ball] The ball is dropping into the pockets.");
            }

            // Update position
            Position += AngularVelocity * deltaTime;
            Position %= 360;

            // Collision detection
            if (hasDropped)
            {
                CheckCollisions(deltaTime);
                ApplyRollingResistance(deltaTime);
            }

            // Check stopping condition
            if (AngularVelocity <= StoppingThreshold)
            {
                AngularVelocity = 0;
                IsStopped = true;
                Console.WriteLine("[Ball] The ball has stopped.");
            }
        }

        private void CheckCollisions(double deltaTime)
        {
            foreach (var fret in _wheel.Frets)
            {
                var (fretStart, fretEnd) = fret.GetLinePoints(_wheel.Position);

                // Ball position in Cartesian coordinates
                double angleRad = Position * Math.PI / 180.0;
                double ballX = BallRadius * Math.Cos(angleRad);
                double ballY = BallRadius * Math.Sin(angleRad);
                var ballPosition = new Vector2D(ballX, ballY);

                // Check collision
                double distance = Vector2D.DistancePointToSegment(ballPosition, fretStart, fretEnd);
                if (distance <= Radius)
                {
                    HandleCollision(fret, fretStart, fretEnd, deltaTime);
                    Console.WriteLine($"[Ball] Collision with fret at angle: {fret.Angle:F2}°");
                    break;
                }
            }
        }

        private void HandleCollision(Fret fret, Vector2D fretStart, Vector2D fretEnd, double deltaTime)
        {
            // Calculate normal vector
            Vector2D fretVector = fretEnd - fretStart;
            Vector2D normal = new Vector2D(-fretVector.Y, fretVector.X).Normalize();

            // Convert angular velocity to linear velocity
            double ballSpeed = AngularVelocity * (Math.PI / 180.0) * BallRadius; // Convert deg/s to units/s
            Vector2D velocity = new Vector2D(-ballSpeed * Math.Sin(Position * Math.PI / 180.0),
                                              ballSpeed * Math.Cos(Position * Math.PI / 180.0));

            // Decompose velocity into normal and tangential components
            double vNormal = velocity.Dot(normal);
            Vector2D vNormalVec = normal * vNormal;
            Vector2D vTangentVec = velocity - vNormalVec;

            // Apply coefficient of restitution
            double restitutionCoefficient = 0.3; // e.g., 0.3 for inelastic collision
            vNormalVec = vNormalVec * (-restitutionCoefficient);

            // Apply friction to tangential component
            double frictionCoefficient = 0.1; // Adjust as needed
            vTangentVec = vTangentVec * (1 - frictionCoefficient);

            // Calculate new velocity
            Vector2D newVelocity = vNormalVec + vTangentVec;

            // Update angular velocity based on new speed
            double newSpeed = newVelocity.Length();
            AngularVelocity = (newSpeed / BallRadius) * (180.0 / Math.PI); // Convert back to deg/s

            // Update position based on new velocity direction
            double newAngleRad = Math.Atan2(newVelocity.Y, newVelocity.X);
            Position = (newAngleRad * 180.0 / Math.PI + 360) % 360;
        }

        private void ApplyRollingResistance(double deltaTime)
        {
            double rollingResistanceCoefficient = 0.01; // Adjust as needed
            double rollingResistanceTorque = rollingResistanceCoefficient * BallRadius * Mass * 9.81; // Mass * g
            double angularDeceleration = rollingResistanceTorque / MomentOfInertia;
            AngularVelocity -= angularDeceleration * deltaTime;
            if (AngularVelocity < 0)
                AngularVelocity = 0;
        }

        public void Draw()
        {
            // Simplified output
            Console.WriteLine($"Ball Position: {Position:F2}°, Angular Velocity: {AngularVelocity:F2}°/s");
        }
    }
}