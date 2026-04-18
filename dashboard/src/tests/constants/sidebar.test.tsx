import React from 'react';
import { render } from '@testing-library/react';
import { tenantDashboardRoutes, adminDashboardRoutes } from '@/constants/sidebar';
import { paths } from '@/constants/constant';

// Mock the icons since we don't need to test their actual rendering
jest.mock('@ant-design/icons', () => ({
  HomeOutlined: () => <div data-testid="home-icon">HomeIcon</div>,
  ShareAltOutlined: () => <div data-testid="share-icon">ShareIcon</div>,
  ApartmentOutlined: () => <div data-testid="apartment-icon">ApartmentIcon</div>,
  BranchesOutlined: () => <div data-testid="branches-icon">BranchesIcon</div>,
  TagOutlined: () => <div data-testid="tag-icon">TagIcon</div>,
  ApiOutlined: () => <div data-testid="api-icon">ApiIcon</div>,
  CrownOutlined: () => <div data-testid="crown-icon">CrownIcon</div>,
  TeamOutlined: () => <div data-testid="team-icon">TeamIcon</div>,
  TagsOutlined: () => <div data-testid="tags-icon">TagsIcon</div>,
  RadarChartOutlined: () => <div data-testid="radar-icon">RadarIcon</div>,
  LockOutlined: () => <div data-testid="lock-icon">LockIcon</div>,
  SaveOutlined: () => <div data-testid="save-icon">SaveIcon</div>,
  HistoryOutlined: () => <div data-testid="history-icon">HistoryIcon</div>,
  SafetyCertificateOutlined: () => <div data-testid="safety-icon">SafetyIcon</div>,
}));

describe('Sidebar Constants', () => {
  describe('tenantDashboardRoutes', () => {
    it('should have the correct number of routes', () => {
      expect(tenantDashboardRoutes).toHaveLength(9);
    });

    it('should have all required route properties', () => {
      tenantDashboardRoutes.forEach((route) => {
        expect(route).toHaveProperty('key');
        expect(route).toHaveProperty('icon');
        expect(route).toHaveProperty('label');
        expect(route).toHaveProperty('path');
      });
    });

    it('should have correct route keys', () => {
      const keys = tenantDashboardRoutes.map((route) => route.key);
      expect(keys).toEqual([
        '/',
        '/graphs',
        '/nodes',
        '/edges',
        '/labels',
        '/tags',
        '/vectors',
        '/request-history',
        '/api-explorer',
      ]);
    });

    it('should have correct labels', () => {
      const labels = tenantDashboardRoutes.map((route) => route.label);
      expect(labels).toEqual([
        'Home',
        'Graphs',
        'Nodes',
        'Edges',
        'Labels',
        'Tags',
        'Vectors',
        'Requests',
        'API Explorer',
      ]);
    });

    it('should have correct paths', () => {
      expect(tenantDashboardRoutes[0].path).toBe(paths.dashboardHome);
      expect(tenantDashboardRoutes[1].path).toBe(paths.graphs);
      expect(tenantDashboardRoutes[2].path).toBe(paths.nodes);
      expect(tenantDashboardRoutes[3].path).toBe(paths.edges);
      expect(tenantDashboardRoutes[4].path).toBe(paths.labels);
      expect(tenantDashboardRoutes[5].path).toBe(paths.tags);
      expect(tenantDashboardRoutes[6].path).toBe(paths.vectors);
      expect(tenantDashboardRoutes[7].path).toBe(paths.requestHistory);
      expect(tenantDashboardRoutes[8].path).toBe(paths.apiExplorer);
    });

    it('should render icons correctly', () => {
      tenantDashboardRoutes.forEach((route) => {
        const { getByTestId } = render(route.icon);
        expect(getByTestId).toBeDefined();
      });
    });
  });

  describe('adminDashboardRoutes', () => {
    it('should have the correct number of routes', () => {
      expect(adminDashboardRoutes).toHaveLength(7);
    });

    it('should have all required route properties', () => {
      adminDashboardRoutes.forEach((route) => {
        expect(route).toHaveProperty('key');
        expect(route).toHaveProperty('icon');
        expect(route).toHaveProperty('label');
        expect(route).toHaveProperty('path');
      });
    });

    it('should have correct route keys', () => {
      const keys = adminDashboardRoutes.map((route) => route.key);
      expect(keys).toEqual([
        '/',
        '/users',
        '/credentials',
        '/authorization',
        '/backups',
        '/request-history',
        '/api-explorer',
      ]);
    });

    it('should have correct labels', () => {
      const labels = adminDashboardRoutes.map((route) => route.label);
      expect(labels).toEqual([
        'Tenants',
        'Users',
        'Credentials',
        'Authorization',
        'Backups',
        'Requests',
        'API Explorer',
      ]);
    });

    it('should have correct paths', () => {
      expect(adminDashboardRoutes[0].path).toBe(paths.adminDashboard);
      expect(adminDashboardRoutes[1].path).toBe(paths.users);
      expect(adminDashboardRoutes[2].path).toBe(paths.credentials);
      expect(adminDashboardRoutes[3].path).toBe(paths.authorization);
      expect(adminDashboardRoutes[4].path).toBe(paths.backups);
      expect(adminDashboardRoutes[5].path).toBe(paths.adminRequestHistory);
      expect(adminDashboardRoutes[6].path).toBe(paths.adminApiExplorer);
    });

    it('should render icons correctly', () => {
      adminDashboardRoutes.forEach((route) => {
        const { getByTestId } = render(route.icon);
        expect(getByTestId).toBeDefined();
      });
    });
  });
});
