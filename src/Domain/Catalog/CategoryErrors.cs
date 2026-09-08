// <copyright file="CategoryErrors.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SharedKernel.Result;

namespace Domain.Catalog;

public static class CategoryErrors
{
    public static Error IdRequired => Error.Validation(
        "Categories.IdRequired",
        $"The category Id is required");

    public static Error NameRequired => Error.Validation(
        "Categories.NameRequired",
        $"اسم الفئة مطلوب");

    public static Error NotFound(Guid categoryId) => Error.NotFound(
        "Categories.NotFound",
        $"الفئة بـ Id = '{categoryId}' غير موجودة");

    public static readonly Error DuplicateName = Error.Conflict(
        "Categories.DuplicateName",
        "اسم الفئة موجود بالفعل على هذا المستوى");

    public static readonly Error HasChildren = Error.Conflict(
        "Categories.HasChildren",
        "لا يمكن تعطيل فئة تحتوي على فئات فرعية نشطة");

    public static readonly Error CircularReference = Error.Conflict(
        "Categories.CircularReference",
        "لا يمكن أن تكون الفئة والمصدر نفسه.");

    public static Error InvalidWeight => Error.Validation(
        "Categories.InvalidWeight",
        "وزن الفئة مطلوب ويجب أن يكون أكبر من صفر");

    public static Error InvalidKarat => Error.Validation(
        "Categories.InvalidKarat",
        "عيار الفئة مطلوب");

    public static readonly Error HasReferences = Error.Conflict(
        "Categories.HasReferences",
        "لا يمكن حذف فئة مستخدمة في فواتير البيع أو الشراء");

    public static Error InsufficientWeight(decimal available, decimal required) => Error.Conflict(
        "Categories.InsufficientWeight",
        $"وزن التصنيف غير كافٍ: المتاح {available:N3} غ والمطلوب {required:N3} غ");
}
