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
            settings.food.maxPlantsPerTile = 0;
            var world = new WorldSimulation(settings, 1);
            var creature = world.SpawnCreature(System.Numerics.Vector2.Zero);
            world.Food.Spawn(FoodKind.Plant, new System.Numerics.Vector2(1f, 0f));
            world.Food.Spawn(FoodKind.Plant, new System.Numerics.Vector2(-1f, 0f));

            for (var i = 0; i < 60; i++)
            {
                world.Step(1f / 60f);
            }

            Assert.That(creature.Intake, Is.EqualTo(2));
            Assert.That(creature.Size, Is.GreaterThan(1f));
            Assert.That(world.Food.Items, Is.Empty);
        }

        [Test]
        public void World_StarvationRemovesCreatureAndKeepsScore()
        {
            var settings = new WorldSettings();
            settings.food.maxPlantsPerTile = 0;
            settings.hunger.decayPerSecond = 10f;
            var world = new WorldSimulation(settings, 1);
            var creature = world.SpawnCreature(System.Numerics.Vector2.Zero);
            creature.AddIntake(12, settings.growth);
            creature.SetSatiety(1f);
            var died = 0;
            world.CreatureDied += death => died++;

            world.Step(0.2f);

            Assert.That(died, Is.EqualTo(1));
            Assert.That(creature.IsAlive, Is.False);
            Assert.That(creature.Intake, Is.EqualTo(12));
            Assert.That(world.Creatures, Is.Empty);
        }

        [Test]
        public void CameraZoom_WidensWithSizeWithinLimits()
        {
            var zoom = new CameraZoomSettings();

            var small = WorldCameraController.GetTargetHalfExtent(1f, zoom);
            var medium = WorldCameraController.GetTargetHalfExtent(3f, zoom);
            var huge = WorldCameraController.GetTargetHalfExtent(1000f, zoom);

            Assert.That(small, Is.EqualTo(zoom.baseHalfExtent).Within(1e-4f));
            Assert.That(medium, Is.GreaterThan(small));
            Assert.That(huge, Is.EqualTo(zoom.maxHalfExtent).Within(1e-4f));
        }

        [Test]
        public void CameraZoom_ShortSideGetsBaseRange()
        {
            // 가로 화면(16:9)은 세로가 짧은 변이고, 세로 화면(9:16)은 가로가 짧은 변입니다.
            var landscape = WorldCameraController.GetOrthographicSize(10f, 16f / 9f);
            var portrait = WorldCameraController.GetOrthographicSize(10f, 9f / 16f);

            Assert.That(landscape, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(landscape * 16f / 9f, Is.GreaterThan(10f));
            Assert.That(portrait * 9f / 16f, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(portrait, Is.GreaterThan(10f));
        }

        [Test]
        public void CameraBounds_StopAtWorldEdgeAndCenterWhenLarger()
        {
            Assert.That(WorldCameraController.ClampAxis(3f, 10f, 500f), Is.EqualTo(3f));
            Assert.That(WorldCameraController.ClampAxis(495f, 10f, 500f), Is.EqualTo(490f));
            Assert.That(WorldCameraController.ClampAxis(-499f, 10f, 500f), Is.EqualTo(-490f));
            Assert.That(WorldCameraController.ClampAxis(4f, 30f, 20f), Is.EqualTo(0f));
        }

        [Test]
        public void CameraZoom_SmoothingConvergesWithoutOvershoot()
        {
            var value = 8f;
            for (var i = 0; i < 600; i++)
            {
                value = WorldCameraController.SmoothHalfExtent(value, 12f, 0.4f, 1f / 60f);
                Assert.That(value, Is.LessThanOrEqualTo(12f));
            }

            Assert.That(value, Is.EqualTo(12f).Within(1e-3f));
        }
    }
}
