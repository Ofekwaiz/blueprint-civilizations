using System;
using System.Linq;
using BlueprintCivilizations.Content.Catalogs;
using BlueprintCivilizations.Content.Definitions;
using BlueprintCivilizations.Content.Validation;
using BlueprintCivilizations.Core;
using NUnit.Framework;
using UnityEngine;

namespace BlueprintCivilizations.Content.Tests
{
    public sealed class ContentFoundationTests
    {
        [Test]
        public void CatalogRejectsDuplicateIds()
        {
            var a = ScriptableObject.CreateInstance<UnitDefinition>(); a.EditorInitialize("unit.test.same", "A");
            var b = ScriptableObject.CreateInstance<UnitDefinition>(); b.EditorInitialize("unit.test.same", "B");
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>(); catalog.EditorSetDefinitions(new ContentDefinition[]{a,b});
            Assert.Throws<InvalidOperationException>(() => catalog.RebuildIndex());
        }

        [Test]
        public void CatalogResolvesByStableId()
        {
            var unit = ScriptableObject.CreateInstance<UnitDefinition>(); unit.EditorInitialize("unit.test.one", "One");
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>(); catalog.EditorSetDefinitions(new[]{unit});
            Assert.That(catalog.TryGet<UnitDefinition>("unit.test.one", out var result), Is.True);
            Assert.That(result, Is.SameAs(unit));
        }

        [Test]
        public void UnitValidationReportsMissingRace()
        {
            var unit = ScriptableObject.CreateInstance<UnitDefinition>(); unit.EditorInitialize("unit.test.no_race", "No Race");
            Assert.That(ContentValidator.Validate(unit).Any(i => i.Message.Contains("race")), Is.True);
        }

        [Test]
        public void RuntimeStateDoesNotMutateDefinition()
        {
            var unit = ScriptableObject.CreateInstance<UnitDefinition>(); unit.EditorInitialize("unit.test.spider", "Spider");
            var state = new UnitBlueprintState("player.1", unit.Id);
            state.PurchaseCopy(StatUpgradeType.MaxHealth);
            Assert.That(unit.Id, Is.EqualTo("unit.test.spider"));
            Assert.That(state.CopiesPurchased, Is.EqualTo(2));
        }
    }
}
