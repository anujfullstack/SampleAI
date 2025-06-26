<script setup lang="ts">
import { ref, reactive } from 'vue'
import axios from 'axios'
import QueryInput from './components/QueryInput.vue'
import ResultsDisplay from './components/ResultsDisplay.vue'
import ChartDisplay from './components/ChartDisplay.vue'
import SqlPanel from './components/SqlPanel.vue'
import ErrorAlert from './components/ErrorAlert.vue'
import LoadingSpinner from './components/LoadingSpinner.vue'
import SampleQueries from './components/SampleQueries.vue'

interface QueryResult {
  data: any[]
  sql: string
  tokenUsage?: {
    prompt: number
    completion: number
    total: number
  }
}

const query = ref('')
const projectId = ref(123)
const loading = ref(false)
const error = ref('')
const result = reactive<QueryResult>({
  data: [],
  sql: '',
  tokenUsage: undefined
})

const handleQuery = async () => {
  if (!query.value.trim()) {
    error.value = 'Please enter a query'
    return
  }

  loading.value = true
  error.value = ''
  
  try {
    const response = await axios.post('https://localhost:56875/api/Query/ask', {
      projectId: projectId.value,
      query: query.value
    })
    
    result.data = response.data.data
    result.sql = response.data.sql
    result.tokenUsage = response.data.tokenUsage
  } catch (err: any) {
    error.value = err.response?.data?.error || 'An error occurred while processing your query'
  } finally {
    loading.value = false
  }
}

const clearResults = () => {
  result.data = []
  result.sql = ''
  result.tokenUsage = undefined
  error.value = ''
}

const handleSampleQuery = (sampleQuery: string) => {
  query.value = sampleQuery
}
</script>

<template>
  <div class="min-h-screen bg-gradient-to-br from-slate-50 via-blue-50 to-indigo-100">
    <!-- Header -->
    <header class="bg-white/80 backdrop-blur-sm border-b border-slate-200 sticky top-0 z-50">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
        <div class="flex items-center justify-between">
          <div class="flex items-center space-x-4">
            <div class="flex items-center space-x-3">
              <div class="w-10 h-10 bg-gradient-to-br from-blue-500 to-indigo-600 rounded-xl flex items-center justify-center">
                <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v4a2 2 0 01-2 2h-2a2 2 0 01-2-2z"/>
                </svg>
              </div>
              <div>
                <h1 class="text-2xl font-bold bg-gradient-to-r from-blue-600 to-indigo-600 bg-clip-text text-transparent">
                  Ventla NQL
                </h1>
                <p class="text-sm text-slate-600">Natural Query Language</p>
              </div>
            </div>
          </div>
          <div class="flex items-center space-x-4">
            <div class="hidden md:flex items-center space-x-2 text-sm text-slate-600">
              <div class="w-2 h-2 bg-green-400 rounded-full animate-pulse"></div>
              <span>Connected</span>
            </div>
          </div>
        </div>
      </div>
    </header>

    <!-- Main Content -->
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div class="grid grid-cols-1 lg:grid-cols-4 gap-8">
        <!-- Main Content Area -->
        <div class="lg:col-span-3 space-y-8">
          <!-- Hero Section -->
          <div class="text-center mb-8">
            <h2 class="text-4xl font-bold text-slate-800 mb-4">
              Ask Questions in Plain English
            </h2>
            <p class="text-lg text-slate-600 max-w-3xl mx-auto">
              Transform your natural language questions into powerful SQL queries and get instant insights with interactive visualizations
            </p>
          </div>

          <!-- Query Input Section -->
          <QueryInput
            v-model:query="query"
            v-model:project-id="projectId"
            :loading="loading"
            @submit="handleQuery"
            @clear="clearResults"
          />

          <!-- Loading State -->
          <LoadingSpinner v-if="loading" />

          <!-- Error State -->
          <ErrorAlert v-if="error" :message="error" @dismiss="error = ''" />

          <!-- Results Section -->
          <ResultsDisplay 
            v-if="result.data.length > 0 && !loading" 
            :data="result.data" 
            :token-usage="result.tokenUsage" 
          />

          <!-- SQL Panel -->
          <SqlPanel v-if="result.sql && !loading" :sql="result.sql" />

          <!-- Chart Display -->
          <ChartDisplay 
            v-if="result.data.length > 0 && !loading" 
            :data="result.data" 
            :title="`Chart: ${query}`" 
          />
        </div>

        <!-- Sidebar -->
        <div class="lg:col-span-1">
          <SampleQueries @select-query="handleSampleQuery" />
        </div>
      </div>
    </div>

    <!-- Footer -->
    <footer class="bg-white/50 backdrop-blur-sm border-t border-slate-200 mt-16">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        <div class="text-center text-sm text-slate-600">
          <p>&copy; 2025 Ventla NQL. Powered by AI-driven natural language processing.</p>
        </div>
      </div>
    </footer>
  </div>
</template>