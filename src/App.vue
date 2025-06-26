<script setup lang="ts">
import { ref, reactive } from 'vue'
import axios from 'axios'
import QueryInput from './components/QueryInput.vue'
import ResultsDisplay from './components/ResultsDisplay.vue'
import ChartDisplay from './components/ChartDisplay.vue'
import SqlPanel from './components/SqlPanel.vue'
import ErrorAlert from './components/ErrorAlert.vue'
import LoadingSpinner from './components/LoadingSpinner.vue'

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
</script>

<template>
  <div class="min-h-screen py-8 px-4 sm:px-6 lg:px-8">
    <div class="max-w-7xl mx-auto">
      <!-- Header -->
      <div class="text-center mb-8">
        <h1 class="text-4xl font-bold text-slate-800 mb-4">
          Natural Language to SQL
        </h1>
        <p class="text-lg text-slate-600 max-w-2xl mx-auto">
          Ask questions about your data in plain English and get SQL-powered answers with interactive charts
        </p>
      </div>

      <!-- Main Content -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <!-- Query Input Section -->
        <div class="lg:col-span-3">
          <QueryInput
            v-model:query="query"
            v-model:project-id="projectId"
            :loading="loading"
            @submit="handleQuery"
            @clear="clearResults"
          />
        </div>

        <!-- Loading State -->
        <div v-if="loading" class="lg:col-span-3">
          <LoadingSpinner />
        </div>

        <!-- Error State -->
        <div v-if="error" class="lg:col-span-3">
          <ErrorAlert :message="error" @dismiss="error = ''" />
        </div>

        <!-- Results Section -->
        <div v-if="result.data.length > 0 && !loading" class="lg:col-span-3">
          <ResultsDisplay :data="result.data" :token-usage="result.tokenUsage" />
        </div>

        <!-- SQL Panel -->
        <div v-if="result.sql && !loading" class="lg:col-span-3">
          <SqlPanel :sql="result.sql" />
        </div>

        <!-- Chart Display -->
        <div v-if="result.data.length > 0 && !loading" class="lg:col-span-3">
          <ChartDisplay :data="result.data" :title="`Chart: ${query}`" />
        </div>
      </div>
    </div>
  </div>
</template>