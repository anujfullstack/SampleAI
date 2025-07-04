<template>
  <div class="card">
    <div class="flex items-center justify-center py-12">
      <div class="text-center">
        <div class="loading-spinner mx-auto mb-4"></div>
        <p class="text-slate-600">Processing your query...</p>
        <p class="text-sm text-slate-500 mt-2">This may take a few moments</p>
        
        <!-- Loading skeleton for better UX -->
        <div class="mt-6 space-y-3">
          <div class="h-4 bg-slate-200 rounded animate-pulse"></div>
          <div class="h-4 bg-slate-200 rounded animate-pulse w-3/4 mx-auto"></div>
          <div class="h-4 bg-slate-200 rounded animate-pulse w-1/2 mx-auto"></div>
        </div>
      </div>
    </div>
  </div>
</template>