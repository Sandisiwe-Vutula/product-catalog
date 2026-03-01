import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';

import { ProductService } from './product.service';
import { Product, PagedResult } from '../models/models';
import { environment } from '../../environments/environment';

const BASE = `${environment.apiUrl}/products`;

/** Minimal product stub */
const mockProduct = (): Product => ({
  id: 1, name: 'Laptop', description: null, sku: 'LAP-001',
  price: 1299.99, quantity: 10, categoryId: 2,
  categoryName: 'Computers', inStock: true,
  createdAt: '2024-01-01T00:00:00Z', updatedAt: '2024-01-01T00:00:00Z'
});

const mockPage = (): PagedResult<Product> => ({
  items: [mockProduct()], totalCount: 1, page: 1, pageSize: 12, totalPages: 1
});

describe('ProductService', () => {
  let service: ProductService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ProductService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(ProductService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());  // assert no unexpected requests

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getProducts() should call GET /api/products with no params', () => {
    service.getProducts().subscribe(result => {
      expect(result.items.length).toBe(1);
      expect(result.items[0].name).toBe('Laptop');
    });

    const req = http.expectOne(BASE);
    expect(req.request.method).toBe('GET');
    req.flush(mockPage());
  });

  it('getProducts() should include search and categoryId query params', () => {
    service.getProducts({ search: 'lap', categoryId: 2, page: 1, pageSize: 12 }).subscribe();

    const req = http.expectOne(r =>
      r.url === BASE &&
      r.params.get('search')     === 'lap' &&
      r.params.get('categoryId') === '2'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockPage());
  });

  it('getProduct() should call GET /api/products/:id', () => {
    service.getProduct(1).subscribe(p => {
      expect(p.id).toBe(1);
      expect(p.sku).toBe('LAP-001');
    });

    const req = http.expectOne(`${BASE}/1`);
    expect(req.request.method).toBe('GET');
    req.flush(mockProduct());
  });

  it('createProduct() should POST and return the saved product', () => {
    const payload = {
      name: 'Laptop', description: null, sku: 'LAP-001',
      price: 1299.99, quantity: 10, categoryId: 2
    };

    service.createProduct(payload).subscribe(p => {
      expect(p.id).toBe(1);
    });

    const req = http.expectOne(BASE);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush(mockProduct());
  });

  it('updateProduct() should PUT to /api/products/:id', () => {
    const payload = {
      name: 'Laptop Pro', description: null, sku: 'LAP-001',
      price: 1499.99, quantity: 8, categoryId: 2
    };

    service.updateProduct(1, payload).subscribe(p => {
      expect(p.name).toBe('Laptop');  // server returns the flushed mock
    });

    const req = http.expectOne(`${BASE}/1`);
    expect(req.request.method).toBe('PUT');
    req.flush(mockProduct());
  });

  it('deleteProduct() should call DELETE /api/products/:id', () => {
    service.deleteProduct(1).subscribe(() => { /* void */ });

    const req = http.expectOne(`${BASE}/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
