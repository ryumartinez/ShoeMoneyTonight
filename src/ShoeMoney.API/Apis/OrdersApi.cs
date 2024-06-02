﻿using Microsoft.EntityFrameworkCore;
using static Microsoft.AspNetCore.Http.TypedResults;
using ShoeMoney.Data;
using ShoeMoney.Data.Entities;

using WilderMinds.MinimalApiDiscovery;
using MinimalApis.FluentValidation;
using EFCore.BulkExtensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Mapster;

namespace ShoeMoney.API.Apis;

public class OrdersApi : IApi
{
  public void Register(IEndpointRouteBuilder builder)
  {
    var group = builder.MapGroup("/api/orders");

    group.MapGet("", GetAllOrders)
      .Produces<IEnumerable<Order>>()
      .ProducesProblem(500);

    group.MapGet("{id:int}", GetOrder)
      .Produces<Order>()
      .ProducesProblem(500)
      .ProducesProblem(404);

    group.MapPost("", CreateOrder)
      .Validate<Order>()
      .Produces<Order>()
      .Produces(201)
      .ProducesProblem(400)
      .ProducesProblem(500);

    group.MapPut("{id:int}", UpdateOrder)
      .Validate<Order>()
      .Produces<Order>()
      .Produces(200)
      .ProducesProblem(400)
      .ProducesProblem(500);

    group.MapDelete("{id:int}", DeleteOrder)
      .Produces(200)
      .ProducesProblem(404)
      .ProducesProblem(500);
  }

  public static async Task<IResult> GetAllOrders(ShoeContext context)
  {
    var orders = await context.Orders
      .Include(o => o.Items)
      .ThenInclude(i => i.Product)
      .Include(o => o.ShippingAddress)
      .Include(o => o.Payment)
      .OrderBy(o => o.OrderDate)
      .ToListAsync();

    return Ok(orders);
  }

  public static async Task<IResult> GetOrder(ShoeContext context, int id)
  {
    var order = await context.Orders
      .Include(o => o.Items)
      .ThenInclude(i => i.Product)
      .Include(o => o.ShippingAddress)
      .Include(o => o.Payment)
      .Where(o => o.Id == id)
      .FirstOrDefaultAsync();

    if (order is null) return NotFound();

    return Ok(order);
  }

  public static async Task<IResult> CreateOrder(ShoeContext context, Order model)
  {
    // Remove the products so we don't try to insert them
    foreach (var item in model.Items)
    {
      if (item.Product is not null) item.Product = null;
    }

    context.Add(model);

    if (await context.SaveChangesAsync() > 0)
    {
      return Created($"/api/orders/{model.Id}", model);
    }

    return BadRequest();
  }

  public static async Task<IResult> UpdateOrder(ShoeContext context, int id, Order model)
  {
    if (id != model.Id) BadRequest("Ids do not match");

    if (model.Items.Any())
    {
      foreach (var item in model.Items) item.OrderId = id;
    }

    var order = await context.Orders
      .Include(o => o.ShippingAddress)
      .Include(o => o.Payment)
      .Include(o => o.Items)
      .Where(o => o.Id == id)
      .FirstOrDefaultAsync();

    if (order is null) return NotFound();

    model.BuildAdapter()
      .EntityFromContext(context)
      .AdaptTo(order);

    if (await context.SaveChangesAsync() > 0)
    {
      return Ok();
    }

    return BadRequest();
  }

  public static async Task<IResult> DeleteOrder(ShoeContext context, int id)
  {
    var result = await context.Orders
      .Where(o => o.Id == id)
      .ExecuteDeleteAsync();

    return result > 0 ? Ok() : NotFound();

  }


}
