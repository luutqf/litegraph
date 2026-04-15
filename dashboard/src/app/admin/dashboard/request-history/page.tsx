import RequestHistoryPage from '@/page/request-history/RequestHistoryPage';
import { Metadata } from 'next';
import React from 'react';

export const metadata: Metadata = {
  title: 'LiteGraph | Request History',
  description: 'LiteGraph',
};

const Page = () => {
  return <RequestHistoryPage mode="admin" />;
};

export default Page;
