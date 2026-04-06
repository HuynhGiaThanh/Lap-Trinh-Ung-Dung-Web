Trong file 1 layout phai co renderbody() va chi 1 renderbody()
Tạo Solution có tên SV<MaSV> (vd: SV21T1020001)

Bổ sung cho Solution các Project sau:

SolutionName.Admin: project dạng ASP.NET Core MVC

SolutionName.Shop: project dạng ASP.NET Core MVC

SolutionName.Models: project dạng Class Library

SolutionName.DataLayers: project dạng Class Library

SolutionName.BusinessLayers: project dạng Class Library

Chức năng cho LiteCommerce.Admin
Trang chủ: Home/Index

Account:

Account/Login

Account/Logout

Account/ChangePassword

Supplier:

Supplier/Index

Supplier/Create

Supplier/Edit/{id}

Supplier/Delete/{id}

Customer:

Customer/Index

Customer/Create

Customer/Edit/{id}

Customer/Delete/{id}

Customer/ChangePassword/{id}

Shipper:

Shipper/Index

Shipper/Create

Shipper/Edit/{id}

Shipper/Delete/{id}

Employee:

Employee/Index

Employee/Create

Employee/Edit/{id}

Employee/Delete/{id}

Employee/ChangePassword/{id}

Employee/ChangeRoles/{id}

Category:

Category/Index

Category/Create

Category/Edit/{id}

Category/Delete/{id}

Product:

Product/Index

Tìm kiếm, lọc mặt hàng theo nhà cung cấp, phân loại, khoảng giá, tên

Hiển thị dưới dạng phân trang

Product/Detail/{id}

Product/Create

Product/Edit/{id}

Product/Delete/{id}

Product/ListAttributes/{id}

Product/AddAttribute/{id}

Product/EditAttribute/{id}?attributeId={attributeId}

Product/DeleteAttribute/{id}?attributeId={attributeId}

Product/ListPhotos/{id}

Product/AddPhoto/{id}

Product/EditPhoto/{id}?photoId={photoId}

Product/DeletePhoto/{id}?photoId={photoId}

Order:

Order/Index

Order/Detail/{id}(xem chi tiet don hang va xu ly don hang)

Order/Create