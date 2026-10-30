using Xigenia.Application.Finance;
using Xigenia.Domain;

namespace Xigenia.Tests;

public sealed class FinanceServiceCategoryLifecycleTests
{
    private static readonly Guid GlobalActiveId = new("45454545-4545-4545-4545-454545454545");
    private static readonly Guid GlobalInactiveId = new("46464646-4646-4646-4646-464646464646");
    private static readonly Guid PersonalActiveId = new("47474747-4747-4747-4747-474747474747");
    private static readonly Guid PersonalInactiveId = new("48484848-4848-4848-4848-484848484848");
    private static readonly Guid OtherOwnerCategoryId = new("49494949-4949-4949-4949-494949494949");
    private static readonly Guid MissingCategoryId = new("50505050-5050-5050-5050-505050505050");
    private static readonly Guid PersonalParentId = new("51515151-5151-5151-5151-515151515151");
    private static readonly Guid GlobalParentId = new("52525252-5252-5252-5252-525252525252");
    private static readonly Guid GrandparentId = new("53535353-5353-5353-5353-535353535353");
    private static readonly DateTimeOffset CreatedAt = new(2026, 3, 14, 12, 0, 0, TimeSpan.Zero);
    private const string OwnerId = "owner-1";
    private const string OtherOwnerId = "owner-2";

    [Fact]
    public async Task ListCategories_AppliesVisibilityRules()
    {
        var store = new FakeFinanceStore();
        var globalActive = CreateCategory(GlobalActiveId, ownerId: null, classification: MovementClassification.Income);
        var personalActive = CreateCategory(PersonalActiveId, classification: MovementClassification.Expense);
        var personalInactive = CreateCategory(PersonalInactiveId, isActive: false);
        store.SeedCategory(globalActive);
        store.SeedCategory(CreateCategory(GlobalInactiveId, ownerId: null, isActive: false));
        store.SeedCategory(personalActive);
        store.SeedCategory(personalInactive);
        store.SeedCategory(CreateCategory(OtherOwnerCategoryId, ownerId: OtherOwnerId));
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        var responses = await service.ListFinancialCategoriesAsync(OwnerId, cts.Token);

        var call = Assert.Single(store.CategoryListCalls);
        Assert.Equal(OwnerId, call.OwnerId);
        Assert.Equal(cts.Token, call.CancellationToken);
        Assert.Equal(3, responses.Count);
        AssertCategoryResponseMatches(responses.Single(r => r.Id == GlobalActiveId), globalActive);
        AssertCategoryResponseMatches(responses.Single(r => r.Id == PersonalActiveId), personalActive);
        AssertCategoryResponseMatches(responses.Single(r => r.Id == PersonalInactiveId), personalInactive);
        Assert.DoesNotContain(responses, r => r.Id == GlobalInactiveId);
        Assert.DoesNotContain(responses, r => r.Id == OtherOwnerCategoryId);
        AssertNoWrites(store);
    }

    [Fact]
    public async Task GetCategory_ReturnsAccessibleCategories()
    {
        var store = new FakeFinanceStore();
        var globalActive = CreateCategory(GlobalActiveId, ownerId: null, classification: MovementClassification.Income);
        var personalActive = CreateCategory(PersonalActiveId, classification: MovementClassification.Expense);
        var personalInactive = CreateCategory(PersonalInactiveId, isActive: false);
        store.SeedCategory(globalActive);
        store.SeedCategory(personalActive);
        store.SeedCategory(personalInactive);
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        var global = await service.GetFinancialCategoryAsync(OwnerId, GlobalActiveId, cts.Token);
        var personal = await service.GetFinancialCategoryAsync(OwnerId, PersonalActiveId, cts.Token);
        var inactive = await service.GetFinancialCategoryAsync(OwnerId, PersonalInactiveId, cts.Token);

        Assert.NotNull(global);
        Assert.True(global.IsGlobal);
        AssertCategoryResponseMatches(global, globalActive);
        Assert.NotNull(personal);
        Assert.False(personal.IsGlobal);
        AssertCategoryResponseMatches(personal, personalActive);
        Assert.NotNull(inactive);
        AssertCategoryResponseMatches(inactive, personalInactive);
        Assert.Equal(3, store.CategoryLookupCalls.Count);
        Assert.Equal(GlobalActiveId, store.CategoryLookupCalls[0].CategoryId);
        Assert.Equal(PersonalActiveId, store.CategoryLookupCalls[1].CategoryId);
        Assert.Equal(PersonalInactiveId, store.CategoryLookupCalls[2].CategoryId);
        Assert.All(store.CategoryLookupCalls, c => Assert.Equal(OwnerId, c.OwnerId));
        Assert.All(store.CategoryLookupCalls, c => Assert.Equal(cts.Token, c.CancellationToken));
        AssertNoWrites(store);
    }

    [Fact]
    public async Task GetCategory_WithInaccessibleCategory_ReturnsNull()
    {
        var store = new FakeFinanceStore();
        store.SeedCategory(CreateCategory(GlobalInactiveId, ownerId: null, isActive: false));
        store.SeedCategory(CreateCategory(OtherOwnerCategoryId, ownerId: OtherOwnerId));
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        var inactiveGlobal = await service.GetFinancialCategoryAsync(OwnerId, GlobalInactiveId, cts.Token);
        var missing = await service.GetFinancialCategoryAsync(OwnerId, MissingCategoryId, cts.Token);
        var foreign = await service.GetFinancialCategoryAsync(OwnerId, OtherOwnerCategoryId, cts.Token);

        Assert.Null(inactiveGlobal);
        Assert.Null(missing);
        Assert.Null(foreign);
        Assert.Equal(3, store.CategoryLookupCalls.Count);
        Assert.Equal(GlobalInactiveId, store.CategoryLookupCalls[0].CategoryId);
        Assert.Equal(MissingCategoryId, store.CategoryLookupCalls[1].CategoryId);
        Assert.Equal(OtherOwnerCategoryId, store.CategoryLookupCalls[2].CategoryId);
        Assert.All(store.CategoryLookupCalls, c => Assert.Equal(OwnerId, c.OwnerId));
        Assert.All(store.CategoryLookupCalls, c => Assert.Equal(cts.Token, c.CancellationToken));
        AssertNoWrites(store);
    }

    [Fact]
    public async Task CreateCategory_WithoutParent_PersistsOwnedCategory()
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var response = await service.CreateFinancialCategoryAsync(
            OwnerId,
            new CreateFinancialCategoryRequest("  Dining  ", MovementClassification.Expense, null),
            CancellationToken.None);

        var captured = Assert.Single(store.AddedCategories);
        Assert.NotEqual(Guid.Empty, captured.Id);
        Assert.Equal(OwnerId, captured.OwnerId);
        Assert.Equal("Dining", captured.Name);
        Assert.Equal(MovementClassification.Expense, captured.Classification);
        Assert.Null(captured.ParentCategoryId);
        Assert.True(captured.IsActive);
        AssertCategoryResponseMatches(response, captured);
        Assert.False(response.IsGlobal);
        Assert.Empty(store.CategoryLookups);
        AssertNoUnrelatedWrites(store);
        Assert.Empty(store.UpdatedCategories);
    }

    [Theory]
    [InlineData(false, MovementClassification.Expense)]
    [InlineData(true, MovementClassification.Income)]
    public async Task CreateCategory_WithValidParent_PersistsChildCategory(bool isGlobalParent, MovementClassification classification)
    {
        Guid parentId = isGlobalParent ? GlobalParentId : PersonalParentId;
        string? parentOwner = isGlobalParent ? null : OwnerId;
        var store = new FakeFinanceStore();
        store.SeedCategory(CreateCategory(parentId, ownerId: parentOwner, classification: classification));
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        var response = await service.CreateFinancialCategoryAsync(
            OwnerId,
            new CreateFinancialCategoryRequest("Child", classification, parentId),
            cts.Token);

        var lookup = Assert.Single(store.CategoryLookupCalls);
        Assert.Equal(parentId, lookup.CategoryId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Equal(cts.Token, lookup.CancellationToken);
        var captured = Assert.Single(store.AddedCategories);
        Assert.Equal(OwnerId, captured.OwnerId);
        Assert.Equal(parentId, captured.ParentCategoryId);
        Assert.Equal(classification, captured.Classification);
        Assert.True(captured.IsActive);
        AssertCategoryResponseMatches(response, captured);
        AssertNoUnrelatedWrites(store);
        Assert.Empty(store.UpdatedCategories);
    }

    [Fact]
    public async Task CreateCategory_WithEmptyParentId_ThrowsArgumentException()
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateFinancialCategoryAsync(
            OwnerId,
            new CreateFinancialCategoryRequest("Child", MovementClassification.Income, Guid.Empty),
            CancellationToken.None));

        Assert.Equal("parentId", ex.ParamName);
        Assert.Empty(store.CategoryLookups);
        AssertNoWrites(store);
    }

    [Fact]
    public async Task CreateCategory_WithMissingParent_ThrowsKeyNotFoundException()
    {
        var store = new FakeFinanceStore();
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateFinancialCategoryAsync(
            OwnerId,
            new CreateFinancialCategoryRequest("Child", MovementClassification.Income, PersonalParentId),
            cts.Token));

        var lookup = Assert.Single(store.CategoryLookupCalls);
        Assert.Equal(PersonalParentId, lookup.CategoryId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Equal(cts.Token, lookup.CancellationToken);
        AssertNoWrites(store);
    }

    [Fact]
    public async Task CreateCategory_WithForeignParent_ThrowsKeyNotFoundException()
    {
        var store = new FakeFinanceStore();
        store.SeedCategory(CreateCategory(PersonalParentId, ownerId: OtherOwnerId));
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateFinancialCategoryAsync(
            OwnerId,
            new CreateFinancialCategoryRequest("Child", MovementClassification.Income, PersonalParentId),
            cts.Token));

        var lookup = Assert.Single(store.CategoryLookupCalls);
        Assert.Equal(PersonalParentId, lookup.CategoryId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Equal(cts.Token, lookup.CancellationToken);
        AssertNoWrites(store);
    }

    [Fact]
    public async Task CreateCategory_WithInactiveParent_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        store.SeedCategory(CreateCategory(PersonalParentId, isActive: false));
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateFinancialCategoryAsync(
            OwnerId,
            new CreateFinancialCategoryRequest("Child", MovementClassification.Income, PersonalParentId),
            cts.Token));

        var lookup = Assert.Single(store.CategoryLookupCalls);
        Assert.Equal(PersonalParentId, lookup.CategoryId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Equal(cts.Token, lookup.CancellationToken);
        AssertNoWrites(store);
    }

    [Fact]
    public async Task CreateCategory_WithNestedParent_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        store.SeedCategory(CreateCategory(PersonalParentId, parentCategoryId: GrandparentId));
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateFinancialCategoryAsync(
            OwnerId,
            new CreateFinancialCategoryRequest("Child", MovementClassification.Income, PersonalParentId),
            cts.Token));

        var lookup = Assert.Single(store.CategoryLookupCalls);
        Assert.Equal(PersonalParentId, lookup.CategoryId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Equal(cts.Token, lookup.CancellationToken);
        AssertNoWrites(store);
    }

    [Fact]
    public async Task CreateCategory_WithClassificationMismatch_ThrowsFinanceRuleException()
    {
        var store = new FakeFinanceStore();
        store.SeedCategory(CreateCategory(PersonalParentId, classification: MovementClassification.Expense));
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        await Assert.ThrowsAsync<FinanceRuleException>(() => service.CreateFinancialCategoryAsync(
            OwnerId,
            new CreateFinancialCategoryRequest("Child", MovementClassification.Income, PersonalParentId),
            cts.Token));

        var lookup = Assert.Single(store.CategoryLookupCalls);
        Assert.Equal(PersonalParentId, lookup.CategoryId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Equal(cts.Token, lookup.CancellationToken);
        AssertNoWrites(store);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RenameCategory_WithMissingOrForeignCategory_ThrowsKeyNotFoundException(bool seededElsewhere)
    {
        var store = new FakeFinanceStore();
        if (seededElsewhere)
        {
            store.SeedCategory(CreateCategory(PersonalActiveId, ownerId: OtherOwnerId));
        }

        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RenameFinancialCategoryAsync(
            OwnerId,
            PersonalActiveId,
            new RenameFinancialCategoryRequest("Updated"),
            cts.Token));

        var lookup = Assert.Single(store.CategoryLookupCalls);
        Assert.Equal(PersonalActiveId, lookup.CategoryId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Equal(cts.Token, lookup.CancellationToken);
        AssertNoWrites(store);
    }

    [Fact]
    public async Task RenameCategory_WithGlobalCategory_ThrowsUnauthorizedAccessException()
    {
        var store = new FakeFinanceStore();
        store.SeedCategory(CreateCategory(GlobalActiveId, ownerId: null));
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RenameFinancialCategoryAsync(
            OwnerId,
            GlobalActiveId,
            new RenameFinancialCategoryRequest("Updated"),
            cts.Token));

        var lookup = Assert.Single(store.CategoryLookupCalls);
        Assert.Equal(GlobalActiveId, lookup.CategoryId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Equal(cts.Token, lookup.CancellationToken);
        AssertNoWrites(store);
    }

    [Fact]
    public async Task RenameCategory_WithOwnedCategory_UpdatesTrimmedName()
    {
        var store = new FakeFinanceStore();
        store.SeedCategory(CreateCategory(PersonalActiveId));
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        var response = await service.RenameFinancialCategoryAsync(
            OwnerId,
            PersonalActiveId,
            new RenameFinancialCategoryRequest("  Updated  "),
            cts.Token);

        var lookup = Assert.Single(store.CategoryLookupCalls);
        Assert.Equal(PersonalActiveId, lookup.CategoryId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Equal(cts.Token, lookup.CancellationToken);
        var captured = Assert.Single(store.UpdatedCategories);
        Assert.Equal("Updated", captured.Name);
        Assert.Equal(PersonalActiveId, captured.Id);
        Assert.Equal(OwnerId, captured.OwnerId);
        Assert.Equal(MovementClassification.Income, captured.Classification);
        Assert.Null(captured.ParentCategoryId);
        var updateCall = Assert.Single(store.CategoryUpdateCalls);
        Assert.Same(captured, updateCall.Category);
        Assert.Equal(cts.Token, updateCall.CancellationToken);
        AssertCategoryResponseMatches(response, captured);
        AssertNoUnrelatedWrites(store);
        Assert.Empty(store.AddedCategories);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetActivation_WithMissingOrForeignCategory_ThrowsKeyNotFoundException(bool seededElsewhere)
    {
        var store = new FakeFinanceStore();
        if (seededElsewhere)
        {
            store.SeedCategory(CreateCategory(PersonalActiveId, ownerId: OtherOwnerId));
        }

        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SetFinancialCategoryActivationAsync(
            OwnerId,
            PersonalActiveId,
            new SetFinancialCategoryActivationRequest(false),
            cts.Token));

        var lookup = Assert.Single(store.CategoryLookupCalls);
        Assert.Equal(PersonalActiveId, lookup.CategoryId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Equal(cts.Token, lookup.CancellationToken);
        AssertNoWrites(store);
    }

    [Fact]
    public async Task SetActivation_WithGlobalCategory_ThrowsUnauthorizedAccessException()
    {
        var store = new FakeFinanceStore();
        store.SeedCategory(CreateCategory(GlobalActiveId, ownerId: null));
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SetFinancialCategoryActivationAsync(
            OwnerId,
            GlobalActiveId,
            new SetFinancialCategoryActivationRequest(false),
            cts.Token));

        var lookup = Assert.Single(store.CategoryLookupCalls);
        Assert.Equal(GlobalActiveId, lookup.CategoryId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Equal(cts.Token, lookup.CancellationToken);
        AssertNoWrites(store);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task SetActivation_WithOwnedCategory_UpdatesRequestedState(bool startActive, bool requestedActive)
    {
        var store = new FakeFinanceStore();
        store.SeedCategory(CreateCategory(PersonalActiveId, isActive: startActive));
        var service = new FinanceService(store);
        using var cts = new CancellationTokenSource();

        var response = await service.SetFinancialCategoryActivationAsync(
            OwnerId,
            PersonalActiveId,
            new SetFinancialCategoryActivationRequest(requestedActive),
            cts.Token);

        var lookup = Assert.Single(store.CategoryLookupCalls);
        Assert.Equal(PersonalActiveId, lookup.CategoryId);
        Assert.Equal(OwnerId, lookup.OwnerId);
        Assert.Equal(cts.Token, lookup.CancellationToken);
        var captured = Assert.Single(store.UpdatedCategories);
        Assert.Equal(requestedActive, captured.IsActive);
        Assert.Equal(PersonalActiveId, captured.Id);
        Assert.Equal(OwnerId, captured.OwnerId);
        Assert.Equal(MovementClassification.Income, captured.Classification);
        Assert.Null(captured.ParentCategoryId);
        var updateCall = Assert.Single(store.CategoryUpdateCalls);
        Assert.Same(captured, updateCall.Category);
        Assert.Equal(cts.Token, updateCall.CancellationToken);
        Assert.Equal(requestedActive, response.IsActive);
        AssertCategoryResponseMatches(response, captured);
        AssertNoUnrelatedWrites(store);
        Assert.Empty(store.AddedCategories);
    }

    private static FinancialCategory CreateCategory(
        Guid id,
        string? ownerId = OwnerId,
        Guid? parentCategoryId = null,
        string name = "Category",
        MovementClassification classification = MovementClassification.Income,
        bool isActive = true) => new(
            id,
            ownerId,
            parentCategoryId,
            name,
            classification,
            isActive,
            CreatedAt);

    private static void AssertCategoryResponseMatches(FinancialCategoryResponse response, FinancialCategory category)
    {
        Assert.Equal(category.Id, response.Id);
        Assert.Equal(category.ParentCategoryId, response.ParentCategoryId);
        Assert.Equal(category.Name, response.Name);
        Assert.Equal(category.Classification, response.Classification);
        Assert.Equal(category.IsActive, response.IsActive);
        Assert.Equal(category.CreatedAt, response.CreatedAt);
        Assert.Equal(category.OwnerId is null, response.IsGlobal);
    }

    private static void AssertNoWrites(FakeFinanceStore store)
    {
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.LinkedMovements);
        Assert.Empty(store.AddedAccounts);
        Assert.Empty(store.AddedObligations);
        Assert.Empty(store.AddedCategories);
        Assert.Empty(store.UpdatedCategories);
    }

    private static void AssertNoUnrelatedWrites(FakeFinanceStore store)
    {
        Assert.Empty(store.AddedMovements);
        Assert.Empty(store.RecordedTransfers);
        Assert.Empty(store.LinkedMovements);
        Assert.Empty(store.AddedAccounts);
        Assert.Empty(store.AddedObligations);
    }
}
