using MutatIO.Cameras;
using MutatIO.Shared.Feeding;
using MutatIO.Shared.World;
using NUnit.Framework;

namespace MutatIO.Tests.EditMode
{
    /// <summary>
    /// Shared 월드 시뮬레이션이 Unity 런타임에서도 같게 동작하는지와 카메라 줌 계산을 확인합니다.
    /// 먹이·성장 규칙의 자세한 경우는 Server/tests의 WorldSimulationTests에서 다룹니다.
    /// </summary>
    public sealed class WorldTests
    {
        [Test]
        public void World_CreatureEatsNearbyPlantAndGrows()
        {
            var settings = new WorldSettings();
            settings.food.maxPlantCount = 0;
            var world = new WorldSimulation(settings, 1);
            var creature = world.SpawnCreature(System.Numerics.Vector2.Zero);
            world.Food.Spawn(FoodKind.Plant, new System.Numerics.Vector2(1f, 0f), 5f);

            for (var i = 0; i < 60; i++)
            {
                world.Step(1f / 60f);
            }

            Assert.That(creature.Intake, Is.EqualTo(5f));
            Assert.That(creature.Size, Is.GreaterThan(1f));
            Assert.That(world.Food.Items, Is.Empty);
        }

        [Test]
        public void CameraZoom_WidensWithSizeWithinLimits()
        {
            var zoom = new CameraZoomSettings();

            var small = WorldCameraController.GetTargetHalfHeight(1f, zoom);
            var medium = WorldCameraController.GetTargetHalfHeight(3f, zoom);
            var huge = WorldCameraController.GetTargetHalfHeight(1000f, zoom);

            Assert.That(small, Is.EqualTo(zoom.baseHalfHeight).Within(1e-4f));
            Assert.That(medium, Is.GreaterThan(small));
            Assert.That(huge, Is.EqualTo(zoom.maxHalfHeight).Within(1e-4f));
        }

        [Test]
        public void CameraZoom_SmoothingConvergesWithoutOvershoot()
        {
            var value = 8f;
            for (var i = 0; i < 600; i++)
            {
                value = WorldCameraController.SmoothHalfHeight(value, 12f, 0.4f, 1f / 60f);
                Assert.That(value, Is.LessThanOrEqualTo(12f));
            }

            Assert.That(value, Is.EqualTo(12f).Within(1e-3f));
        }
    }
}
